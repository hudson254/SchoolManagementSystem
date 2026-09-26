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
    public class StartRequestReviewCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string? ReviewerNotes { get; set; }
    }

    public class StartRequestReviewCommandValidator : AbstractValidator<StartRequestReviewCommand>
    {
        public StartRequestReviewCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    public class StartRequestReviewCommandHandler : IRequestHandler<StartRequestReviewCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<StartRequestReviewCommandHandler> _logger;

        public StartRequestReviewCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<StartRequestReviewCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(StartRequestReviewCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.ReviewRequest, _currentUser.UserId);

            if (req.Status != RequestStatus.Submitted && req.Status != RequestStatus.PendingReview && req.Status != RequestStatus.Returned)
                throw new BusinessRuleException($"Cannot start review from {req.Status} status.");

            req.StartReview(_currentUser.UserId, _currentUser.Username, request.ReviewerNotes);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestReviewStarted", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} review started by {_currentUser.UserId}. Notes: {request.ReviewerNotes ?? string.Empty}");

            _logger.LogInformation("OMS request {RequestNumber} review started", req.RequestNumber);

            // Notify the requester that their request entered review.
            await _notifier.NotifyAsync(
                req.RequesterUserId,
                $"Request under review: {req.RequestNumber}",
                $"Your request '{req.Title}' ({req.RequestNumber}) is now under review.",
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
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ReviewRequestRoles))
                throw new ForbiddenException(OmsPermissions.ReviewRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}