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
    public class AssignRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string AssignedUserId { get; set; } = string.Empty;
        public string? AssignmentReason { get; set; }
    }

    public class AssignRequestCommandValidator : AbstractValidator<AssignRequestCommand>
    {
        public AssignRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
            RuleFor(x => x.AssignedUserId).NotEmpty().WithMessage("Assigned user is required");
        }
    }

    public class AssignRequestCommandHandler : IRequestHandler<AssignRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<AssignRequestCommandHandler> _logger;

        public AssignRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<AssignRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(AssignRequestCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdWithDetailsAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.AssignRequest, _currentUser.UserId);

            if (req.Status != RequestStatus.PendingReview && req.Status != RequestStatus.Submitted)
                throw new BusinessRuleException($"Cannot assign a request in {req.Status} status.");

            req.Assign(request.AssignedUserId, _currentUser.UserId);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestAssigned", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} assigned to {request.AssignedUserId} by {_currentUser.UserId}");

            _logger.LogInformation("OMS request {RequestNumber} assigned to {AssignedUserId}", req.RequestNumber, request.AssignedUserId);

            // Notify the assignee that the request is now theirs.
            await _notifier.NotifyAsync(
                request.AssignedUserId,
                $"Request assigned: {req.RequestNumber}",
                $"Request '{req.Title}' ({req.RequestNumber}) has been assigned to you by {_currentUser.Username ?? _currentUser.UserId}.",
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
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.AssignRequestRoles))
                throw new ForbiddenException(OmsPermissions.AssignRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}