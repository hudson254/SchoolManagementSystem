using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands
{
    public class ReturnRequestForCorrectionCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string? CorrectionReason { get; set; }
    }

    public class ReturnRequestForCorrectionCommandValidator : AbstractValidator<ReturnRequestForCorrectionCommand>
    {
        public ReturnRequestForCorrectionCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
            RuleFor(x => x.CorrectionReason)
                .NotEmpty().WithMessage("Correction reason is required")
                .MaximumLength(1000);
        }
    }

    public class ReturnRequestForCorrectionCommandHandler : IRequestHandler<ReturnRequestForCorrectionCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<ReturnRequestForCorrectionCommandHandler> _logger;

        public ReturnRequestForCorrectionCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<ReturnRequestForCorrectionCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(ReturnRequestForCorrectionCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.ReturnRequest, _currentUser.UserId);

            if (req.Status != RequestStatus.PendingReview && req.Status != RequestStatus.PendingApproval && req.Status != RequestStatus.Assigned)
                throw new BusinessRuleException($"Cannot return a request in {req.Status} status.");

            req.Return(_currentUser.UserId, _currentUser.Username, request.CorrectionReason ?? string.Empty);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestReturned", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} returned for correction by {_currentUser.UserId}. Reason: {request.CorrectionReason}");

            _logger.LogInformation("OMS request {RequestNumber} returned for correction", req.RequestNumber);

            // Notify the requester that corrections are required.
            await _notifier.NotifyAsync(
                req.RequesterUserId,
                $"Request returned for correction: {req.RequestNumber}",
                $"Your request '{req.Title}' ({req.RequestNumber}) was returned for correction by {_currentUser.Username ?? _currentUser.UserId}. Reason: {request.CorrectionReason}",
                req.Id,
                cancellationToken);

            return RequestDto.FromEntity(req);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");
        }

        private void IfForbiddenThrow()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ReturnRequestRoles))
                throw new ForbiddenException(OmsPermissions.ReturnRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}