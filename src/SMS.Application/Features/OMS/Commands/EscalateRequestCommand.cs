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
    public class EscalateRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string? EscalationReason { get; set; }
        public string? EscalateToUserId { get; set; }
    }

    public class EscalateRequestCommandValidator : AbstractValidator<EscalateRequestCommand>
    {
        public EscalateRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
            RuleFor(x => x.EscalationReason)
                .NotEmpty().WithMessage("Escalation reason is required")
                .MaximumLength(1000);
        }
    }

    public class EscalateRequestCommandHandler : IRequestHandler<EscalateRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<EscalateRequestCommandHandler> _logger;

        public EscalateRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<EscalateRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(EscalateRequestCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.EscalateRequest, _currentUser.UserId);

            // Only certain statuses can be escalated
            if (req.Status != RequestStatus.PendingApproval && req.Status != RequestStatus.Assigned)
                throw new BusinessRuleException($"Cannot escalate a request in {req.Status} status.");

            req.Escalate(_currentUser.UserId, _currentUser.Username, request.EscalationReason);

            // If a specific user is provided as escalation target
            if (!string.IsNullOrWhiteSpace(request.EscalateToUserId))
            {
                req.Reassign(request.EscalateToUserId, _currentUser.UserId);
            }

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestEscalated", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} escalated by {_currentUser.UserId}. Reason: {request.EscalationReason}");

            if (!string.IsNullOrWhiteSpace(req.AssignedUserId) && req.AssignedUserId != _currentUser.UserId)
            {
                // Notify the escalation target that the request needs attention.
                await _notifier.NotifyAsync(
                    req.AssignedUserId,
                    $"Request escalated: {req.RequestNumber}",
                    $"Request '{req.Title}' ({req.RequestNumber}) was escalated by {_currentUser.Username ?? _currentUser.UserId}. Reason: {request.EscalationReason}",
                    req.Id,
                    cancellationToken);
            }

            _logger.LogInformation("OMS request {RequestNumber} escalated", req.RequestNumber);

            return RequestDto.FromEntity(req);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");
        }

        private void IfForbiddenThrow()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.EscalateRequestRoles))
                throw new ForbiddenException(OmsPermissions.EscalateRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}