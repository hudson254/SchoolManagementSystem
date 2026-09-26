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
    /// Approves an OMS order (Submitted/PendingApproval → Approved).
    /// Authorization: Administrator tier (OmsAuthorization.ApproveOrderRoles,
    /// OmsPolicy.CanApproveOrder). The creator of an order can never approve it
    /// (open requirement #5) — enforced here and again inside the domain entity.
    /// Remarks are optional (open requirement #15 documents a requirement only
    /// for rejection, not for approval).
    /// </summary>
    public class ApproveOrderCommand : IRequest<OrderDto>
    {
        public Guid OrderId { get; set; }
        public string? Remarks { get; set; }
    }

    public class ApproveOrderCommandValidator : AbstractValidator<ApproveOrderCommand>
    {
        public ApproveOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
            RuleFor(x => x.Remarks)
                .MaximumLength(1000).WithMessage("Approval remarks must not exceed 1000 characters");
        }
    }

    public class ApproveOrderCommandHandler : IRequestHandler<ApproveOrderCommand, OrderDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly ILogger<ApproveOrderCommandHandler> _logger;

        public ApproveOrderCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            ILogger<ApproveOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<OrderDto> Handle(ApproveOrderCommand request, CancellationToken cancellationToken)
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
                    $"Only Submitted or PendingApproval orders can be approved. Current status: {order.Status}.");
            }

            // Open requirement #5: the creator can never approve their own order.
            // Mirrored here so the failure is a 400, not a 500.
            if (string.Equals(order.RequestedByUserId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException(
                    "The creator of an order cannot approve it (open requirement #5).");
            }

            var remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();

            try
            {
                // Transition + history append + audit commit atomically. The Order
                // row carries an xmin row-version token: a stale writer surfaces
                // as DbUpdateConcurrencyException, translated to 409 (never
                // silently swallowed).
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    order.Approve(_currentUser.UserId, _currentUser.Username, remarks);

                    await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await _auditService.LogActivityAsync(
                        "OrderApproved", "Order", order.Id.ToString(),
                        $"Order {order.OrderNumber} approved by {_currentUser.UserId}.");

                    _logger.LogInformation("OMS order {OrderNumber} approved", order.OrderNumber);

                    return OrderDto.FromEntity(order);
                }, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict approving OMS order {OrderId}", request.OrderId);
                throw new ConflictException(
                    "The order was modified by another user. Reload the order and try again.",
                    ex);
            }
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to approve an order.");
            }
        }

        private void IfForbiddenThrow()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ApproveOrderRoles))
            {
                throw new ForbiddenException(OmsPermissions.ApproveOrder, _currentUser.UserId);
            }
        }

        private void EnsureTenantResolved()
        {
            if (ResolveTenantId() == Guid.Empty)
            {
                throw new UnauthorizedException("A resolved tenant context is required to approve an order.");
            }
        }

        private Guid ResolveTenantId()
        {
            var raw = _tenantContext?.TenantId;
            return Guid.TryParse(raw, out var tenantId) ? tenantId : Guid.Empty;
        }
    }
}