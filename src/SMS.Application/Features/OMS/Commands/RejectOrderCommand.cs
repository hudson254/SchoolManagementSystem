using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands
{
    /// <summary>
    /// Rejects an OMS order (Submitted/PendingApproval → Rejected).
    /// Authorization: Administrator tier (OmsAuthorization.ApproveOrderRoles,
    /// OmsPolicy.CanRejectOrder). A rejection remark is required
    /// (open requirement #15 — documented default, not invented).
    /// </summary>
    public class RejectOrderCommand : IRequest<OrderDto>
    {
        public Guid OrderId { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class RejectOrderCommandValidator : AbstractValidator<RejectOrderCommand>
    {
        public RejectOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("A rejection remark is required")
                .MaximumLength(1000).WithMessage("Rejection remarks must not exceed 1000 characters");
        }
    }

    public class RejectOrderCommandHandler : IRequestHandler<RejectOrderCommand, OrderDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly ILogger<RejectOrderCommandHandler> _logger;

        public RejectOrderCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            ILogger<RejectOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<OrderDto> Handle(RejectOrderCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();
            EnsureTenantResolved();

            var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Order", request.OrderId);
            }

            if (order.Status != OrderStatus.Submitted && order.Status != OrderStatus.PendingApproval)
            {
                throw new BusinessRuleException(
                    $"Only Submitted or PendingApproval orders can be rejected. Current status: {order.Status}.");
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    order.Reject(_currentUser.UserId, _currentUser.Username, request.Remarks.Trim());

                    await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await _auditService.LogActivityAsync(
                        "OrderRejected", "Order", order.Id.ToString(),
                        $"Order {order.OrderNumber} rejected by {_currentUser.UserId}. Remarks: {request.Remarks.Trim()}");

                    _logger.LogInformation("OMS order {OrderNumber} rejected", order.OrderNumber);

                    return OrderDto.FromEntity(order);
                }, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict rejecting OMS order {OrderId}", request.OrderId);
                throw new ConflictException(
                    "The order was modified by another user. Reload the order and try again.",
                    ex);
            }
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to reject an order.");
            }
        }

        private void IfForbiddenThrow()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ApproveOrderRoles))
            {
                throw new ForbiddenException(OmsPermissions.RejectOrder, _currentUser.UserId);
            }
        }

        private void EnsureTenantResolved()
        {
            if (ResolveTenantId() == Guid.Empty)
            {
                throw new UnauthorizedException("A resolved tenant context is required to reject an order.");
            }
        }

        private Guid ResolveTenantId()
        {
            var raw = _tenantContext?.TenantId;
            return Guid.TryParse(raw, out var tenantId) ? tenantId : Guid.Empty;
        }
    }
}