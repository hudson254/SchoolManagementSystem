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
    public class RemoveOrderItemCommand : IRequest<OrderItemDto>
    {
        public Guid OrderId { get; set; }
        public Guid ItemId { get; set; }
    }

    public class RemoveOrderItemCommandValidator : AbstractValidator<RemoveOrderItemCommand>
    {
        public RemoveOrderItemCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
            RuleFor(x => x.ItemId).NotEmpty().WithMessage("Item ID is required");
        }
    }

    public class RemoveOrderItemCommandHandler : IRequestHandler<RemoveOrderItemCommand, OrderItemDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly ILogger<RemoveOrderItemCommandHandler> _logger;

        public RemoveOrderItemCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            ILogger<RemoveOrderItemCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<OrderItemDto> Handle(RemoveOrderItemCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Order", request.OrderId);
            }

            if (order.Status != OrderStatus.Draft)
            {
                throw new BusinessRuleException(
                    $"Order items can only be removed while the order is in Draft status. Current status: {order.Status}.");
            }

            var item = order.Items.FirstOrDefault(i => i.Id == request.ItemId);
            if (item == null)
            {
                throw new NotFoundException("OrderItem", request.ItemId);
            }

            // Capture response DTO before removal because FromEntity depends on a valid row context.
            var removed = OrderItemDto.FromEntity(item);

            order.RemoveItem(request.ItemId);

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "OrderItemRemoved", "OrderItem", item.Id.ToString(),
                $"Item {item.Id} removed from order {order.OrderNumber} by {_currentUser.UserId}.");

            _logger.LogInformation("OMS order {OrderNumber} item removed", order.OrderNumber);

            return removed;
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to remove an order item.");
            }
        }
    }
}
