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
    public class ReassignRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string NewAssignedUserId { get; set; } = string.Empty;
        public string? ReassignmentReason { get; set; }
    }

    public class ReassignRequestCommandValidator : AbstractValidator<ReassignRequestCommand>
    {
        public ReassignRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
            RuleFor(x => x.NewAssignedUserId).NotEmpty().WithMessage("New assignee is required");
        }
    }

    public class ReassignRequestCommandHandler : IRequestHandler<ReassignRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<ReassignRequestCommandHandler> _logger;

        public ReassignRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<ReassignRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(ReassignRequestCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.ReassignRequest, _currentUser.UserId);

            var previousAssignee = req.AssignedUserId;
            req.Reassign(request.NewAssignedUserId, _currentUser.UserId, request.ReassignmentReason ?? string.Empty);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestReassigned", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} reassigned from {previousAssignee} to {request.NewAssignedUserId} by {_currentUser.UserId}");

            if (!string.IsNullOrEmpty(request.NewAssignedUserId))
            {
                // Notify the new assignee that the request is now theirs.
                await _notifier.NotifyAsync(
                    request.NewAssignedUserId,
                    $"Request reassigned: {req.RequestNumber}",
                    $"Request '{req.Title}' ({req.RequestNumber}) has been reassigned to you by {_currentUser.Username ?? _currentUser.UserId}.",
                    req.Id,
                    cancellationToken);
            }

            _logger.LogInformation("OMS request {RequestNumber} reassigned to {NewAssignees}", req.RequestNumber, request.NewAssignedUserId);

            return RequestDto.FromEntity(req);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");
        }

        private void IfForbiddenThrow()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ReassignRequestRoles))
                throw new ForbiddenException(OmsPermissions.ReassignRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}