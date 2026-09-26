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
    public class CancelRequestCommand : IRequest<RequestDto>
    {
        public Guid RequestId { get; set; }
        public string? CancellationReason { get; set; }
    }

    public class CancelRequestCommandValidator : AbstractValidator<CancelRequestCommand>
    {
        public CancelRequestCommandValidator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    public class CancelRequestCommandHandler : IRequestHandler<CancelRequestCommand, RequestDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
                private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<CancelRequestCommandHandler> _logger;

        public CancelRequestCommandHandler(
            IRequestRepository requestRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
                        SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            ILogger<CancelRequestCommandHandler> logger)
        {
            _requestRepository = requestRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
                        _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<RequestDto> Handle(CancelRequestCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            var req = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (req == null)
                throw new NotFoundException("Request", request.RequestId);

            if (req.TenantId != ResolveTenantId())
                throw new ForbiddenException(OmsPermissions.CancelRequest, _currentUser.UserId);

            // Determine if user can cancel this request
            var canCancelAny = OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CancelAnyRequestRoles);
            var canCancelOwn = OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CancelOwnRequestRoles);

            if (!canCancelAny && !canCancelOwn)
                throw new ForbiddenException(OmsPermissions.CancelRequest, _currentUser.UserId);

            // Own request cancellation requires ownership
            if (!canCancelAny && req.RequesterUserId != _currentUser.UserId)
                throw new ForbiddenException(OmsPermissions.CancelRequest, _currentUser.UserId);

            if (RequestLifecycle.IsTerminal(req.Status))
                throw new BusinessRuleException($"Cannot cancel a request in {req.Status} status.");

            req.Cancel(_currentUser.UserId, _currentUser.Username, request.CancellationReason);

            await _requestRepository.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "RequestCancelled", "Request", req.Id.ToString(),
                $"Request {req.RequestNumber} cancelled by {_currentUser.UserId}. Reason: {request.CancellationReason ?? string.Empty}");

            _logger.LogInformation("OMS request {RequestNumber} cancelled", req.RequestNumber);

            return RequestDto.FromEntity(req);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required.");
        }

        private Guid ResolveTenantId() => Guid.TryParse(_tenantContext?.TenantId, out var t) ? t : Guid.Empty;
    }
}