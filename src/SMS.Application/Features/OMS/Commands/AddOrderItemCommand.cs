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
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands
{
    public class AddOrderItemCommand : IRequest<OrderItemDto>
    {
        public Guid OrderId { get; set; }
        public string? ItemCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
    {
        public AddOrderItemCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Item description is required")
                .MaximumLength(500);
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero");
            RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Unit price must not be negative");
            RuleFor(x => x.ItemCode).MaximumLength(50);
        }
    }

    public class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, OrderItemDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly ILogger<AddOrderItemCommandHandler> _logger;

        public AddOrderItemCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            ILogger<AddOrderItemCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<OrderItemDto> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
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
                    $"Order items can only be added while the order is in Draft status. Current status: {order.Status}.");
            }

            var item = order.AddItem(
                request.ItemCode,
                request.Description.Trim(),
                request.Quantity,
                request.UnitPrice);

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "OrderItemAdded", "OrderItem", item.Id.ToString(),
                $"Item {item.Id} added to order {order.OrderNumber} by {_currentUser.UserId}.");

            _logger.LogInformation("OMS order {OrderNumber} item added", order.OrderNumber);

            return OrderItemDto.FromEntity(item);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to add an order item.");
            }
        }
    }
}
