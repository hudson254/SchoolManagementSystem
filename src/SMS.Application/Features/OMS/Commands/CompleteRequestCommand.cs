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
    public class CompleteRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string? CompletionNotes { get; set; }
    }

    public class CompleteRequestCommandValidator : AbstractValidator<CompleteRequestCommand>
    {
        public CompleteRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    public class CompleteRequestCommandHandler : IRequestHandler<CompleteRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<CompleteRequestCommandHandler> _logger;

        public CompleteRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<CompleteRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(CompleteRequestCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.CompleteRequest, _currentUser.UserId);

            if (req.Status != RequestStatus.Approved && req.Status != RequestStatus.InProgress)
                throw new BusinessRuleException($"Cannot complete a request in {req.Status} status.");

            req.Complete(_currentUser.UserId, _currentUser.Username, request.CompletionNotes);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestCompleted", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} completed by {_currentUser.UserId}. Notes: {request.CompletionNotes ?? string.Empty}");

            _logger.LogInformation("OMS request {RequestNumber} completed", req.RequestNumber);

            // Notify the requester of completion.
            await _notifier.NotifyAsync(
                req.RequesterUserId,
                $"Request completed: {req.RequestNumber}",
                $"Your request '{req.Title}' ({req.RequestNumber}) has been marked complete by {_currentUser.Username ?? _currentUser.UserId}.",
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
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CompleteRequestRoles))
                throw new ForbiddenException(OmsPermissions.CompleteRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}