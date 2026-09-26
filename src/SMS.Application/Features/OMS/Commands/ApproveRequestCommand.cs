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
    public class ApproveRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string? ApprovalNotes { get; set; }
    }

    public class ApproveRequestCommandValidator : AbstractValidator<ApproveRequestCommand>
    {
        public ApproveRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    public class ApproveRequestCommandHandler : IRequestHandler<ApproveRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<ApproveRequestCommandHandler> _logger;

        public ApproveRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<ApproveRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(ApproveRequestCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.ApproveRequest, _currentUser.UserId);

            // Prevent self-approval
            if (req.RequesterUserId == _currentUser.UserId)
                throw new BusinessRuleException("Requester cannot approve their own request.");

            if (req.Status != RequestStatus.PendingApproval && req.Status != RequestStatus.Assigned)
                throw new BusinessRuleException($"Cannot approve a request in {req.Status} status.");

            req.Approve(_currentUser.UserId, _currentUser.Username, request.ApprovalNotes);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestApproved", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} approved by {_currentUser.UserId}. Notes: {request.ApprovalNotes ?? string.Empty}");

            _logger.LogInformation("OMS request {RequestNumber} approved", req.RequestNumber);

            // Notify the requester and relevant participants of the decision.
            await _notifier.NotifyAsync(
                req.RequesterUserId,
                $"Request approved: {req.RequestNumber}",
                $"Your request '{req.Title}' ({req.RequestNumber}) has been approved by {_currentUser.Username ?? _currentUser.UserId}.",
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
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ApproveRequestRoles))
                throw new ForbiddenException(OmsPermissions.ApproveRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}