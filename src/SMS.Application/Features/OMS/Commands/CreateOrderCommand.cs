using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.OMS.Dtos;
using SMS.Domain.Common;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Commands
{
    public class CreateOrderCommand : IRequest<OrderDto>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Currency { get; set; } = OmsMoney.DefaultCurrency;
        public DateTime? RequiredByDate { get; set; }
        public Guid? RoadAccountId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public string? ItemCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            BuildRules();
        }

        private void BuildRules()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Order title is required")
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MaximumLength(2000);

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required")
                .Length(3).WithMessage("Currency must be a 3-letter ISO-4217 code");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(dto => dto.Description)
                    .NotEmpty().WithMessage("Item description is required")
                    .MaximumLength(500).WithMessage("Item description must not exceed 500 characters");
                item.RuleFor(dto => dto.Quantity)
                    .GreaterThan(0).WithMessage("Item quantity must be greater than zero");
                item.RuleFor(dto => dto.UnitPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("Item unit price must not be negative");
                item.RuleFor(dto => dto.ItemCode)
                    .MaximumLength(50);
            });
        }
    }

    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IOrderRepository orderRepository,
            IOrderNumberGenerator orderNumberGenerator,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            ITenantContext tenantContext,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _orderNumberGenerator = orderNumberGenerator;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to create an order.");
            }

            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CreateOrderRoles))
            {
                throw new ForbiddenException(OmsPermissions.CreateOrder, _currentUser.UserId);
            }

            var tenantId = ResolveTenantId();
            if (tenantId == Guid.Empty)
            {
                throw new UnauthorizedException("A resolved tenant context is required to create an order.");
            }

            if (request.RoadAccountId.HasValue)
            {
                var account = await _unitOfWork.RoadAccounts.GetByIdAsync(request.RoadAccountId.Value, cancellationToken);
                if (account == null)
                {
                    throw new NotFoundException("RoadAccount", request.RoadAccountId.Value);
                }
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var orderNumber = await _orderNumberGenerator.GenerateNextOrderNumberAsync(tenantId, cancellationToken);

                var order = Order.Create(
                    orderNumber,
                    request.Title.Trim(),
                    request.Description?.Trim(),
                    _currentUser.UserId,
                    request.Currency,
                    request.RequiredByDate,
                    request.RoadAccountId);

                foreach (var item in request.Items)
                {
                    order.AddItem(item.ItemCode, item.Description, item.Quantity, item.UnitPrice);
                }

                await _orderRepository.AddAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _auditService.LogActivityAsync(
                    "OrderCreated", "Order", order.Id.ToString(),
                    $"Order {order.OrderNumber} created by {_currentUser.UserId}");

                _logger.LogInformation("OMS order {OrderNumber} created", order.OrderNumber);

                return OrderDto.FromEntity(order);
            }, cancellationToken);
        }

        private Guid ResolveTenantId()
        {
            var raw = _tenantContext?.TenantId;
            return Guid.TryParse(raw, out var tenantId) ? tenantId : Guid.Empty;
        }
    }
}