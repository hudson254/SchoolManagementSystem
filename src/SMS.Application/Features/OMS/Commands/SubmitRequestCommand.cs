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
    public class SubmitRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
    }

    public class SubmitRequestCommandValidator : AbstractValidator<SubmitRequestCommand>
    {
        public SubmitRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty().WithMessage("Request ID is required");
        }
    }

    public class SubmitRequestCommandHandler : IRequestHandler<SubmitRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly IOmsRequestNotifier _notifier;
        private readonly ILogger<SubmitRequestCommandHandler> _logger;

        public SubmitRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            IOmsRequestNotifier notifier,
            ILogger<SubmitRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(SubmitRequestCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.SubmitRequest, _currentUser.UserId);

            if (req.Status != RequestStatus.Draft && req.Status != RequestStatus.Returned)
                throw new BusinessRuleException($"Only Draft or Returned requests can be submitted. Current status: {req.Status}.");

            if (req.Status == RequestStatus.Draft)
                req.Submit(_currentUser.UserId, _currentUser.Username);
            else if (req.Status == RequestStatus.Returned)
                req.Submit(_currentUser.UserId, _currentUser.Username);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestSubmitted", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} submitted by {_currentUser.UserId}.");

            _logger.LogInformation("OMS request {RequestNumber} submitted", req.RequestNumber);

            // Notify the approval queue that work is waiting. Individual
            // approvers are resolved from the existing role store - the request
            // engine deliberately does not know which person will pick it up.
            await _notifier.NotifyRolesAsync(
                OmsAuthorization.ApproveRequestRoles,
                $"Request submitted: {req.RequestNumber}",
                $"Request '{req.Title}' ({req.RequestNumber}) was submitted by {_currentUser.Username ?? _currentUser.UserId} and is awaiting review.",
                req.Id,
                cancellationToken);

            return RequestDto.FromEntity(req);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required to submit a request.");
        }

        private void IfForbiddenThrow()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.SubmitRequestRoles))
                throw new ForbiddenException(OmsPermissions.SubmitRequest, _currentUser.UserId);
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}