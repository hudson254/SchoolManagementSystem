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
    public class SubmitOrderCommand : IRequest<OrderDto>
    {
        public Guid OrderId { get; set; }
        public bool RequireItems { get; set; } = true;
    }

    public class SubmitOrderCommandValidator : AbstractValidator<SubmitOrderCommand>
    {
        public SubmitOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required");
        }
    }

    public class SubmitOrderCommandHandler : IRequestHandler<SubmitOrderCommand, OrderDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;
        private readonly ILogger<SubmitOrderCommandHandler> _logger;

        public SubmitOrderCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext,
            ILogger<SubmitOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<OrderDto> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            IfForbiddenThrow();

            var order = await _orderRepository.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                throw new NotFoundException("Order", request.OrderId);
            }

            if (order.Status != OrderStatus.Draft)
            {
                throw new BusinessRuleException(
                    $"Only Draft orders can be submitted. Current status: {order.Status}.");
            }

            if (request.RequireItems && order.ItemCount == 0)
            {
                throw new BusinessRuleException("A submitted order must contain at least one item.");
            }

            order.Submit(_currentUser.UserId, _currentUser.Username);

            await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync(
                "OrderSubmitted", "Order", order.Id.ToString(),
                $"Order {order.OrderNumber} submitted by {_currentUser.UserId}.");

            _logger.LogInformation("OMS order {OrderNumber} submitted", order.OrderNumber);

            return OrderDto.FromEntity(order);
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to submit an order.");
            }
        }

        private void IfForbiddenThrow()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.CreateOrderRoles))
            {
                throw new ForbiddenException(OmsPermissions.SubmitOrder, _currentUser.UserId);
            }
        }
    }
}
