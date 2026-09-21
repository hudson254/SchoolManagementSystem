using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands
{
    public class CancelOrderCommand : IRequest<OrderDto>
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool CancelAny { get; set; } = false;
    }

    public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
    {
        public CancelOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A cancellation reason is required")
                .MaximumLength(1000);
        }
    }

    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            ILogger<CancelOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Order", request.OrderId);
            }

            EnsureCanCancel(order, request.CancelAny);

            order.Cancel(_currentUser.UserId, _currentUser.Username, request.Reason);

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "OrderCancelled", "Order", order.Id.ToString(),
                $"Order {order.OrderNumber} cancelled by {_currentUser.UserId}. Reason: {request.Reason}");

            _logger.LogInformation("OMS order {OrderNumber} cancelled", order.OrderNumber);

            return OrderDto.FromEntity(order);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to cancel an order.");
            }
        }

        private void EnsureCanCancel(Order order, bool cancelAny)
        {
            if (order.Status == OrderStatus.Approved ||
                order.Status == OrderStatus.Rejected ||
                order.Status == OrderStatus.Cancelled)
            {
                throw new BusinessRuleException(
                    $"Orders in {order.Status} status cannot be cancelled.");
            }

            if (cancelAny)
            {
                if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CancelAnyOrderRoles))
                {
                    throw new ForbiddenException(OmsPermissions.CancelOrder, _currentUser.UserId);
                }
            }
            else
            {
                if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CancelOwnOrderRoles))
                {
                    throw new ForbiddenException(OmsPermissions.CancelOrder, _currentUser.UserId);
                }

                if (!string.Equals(order.RequestedByUserId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ForbiddenException(
                        OmsPermissions.CancelOrder,
                        _currentUser.UserId);
                }
            }
        }
    }
}
