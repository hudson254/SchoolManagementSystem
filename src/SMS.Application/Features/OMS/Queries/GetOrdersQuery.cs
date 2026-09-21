using FluentValidation;
using MediatR;
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

namespace SMS.Application.Features.OMS.Queries
{
    public class GetOrdersQuery : IRequest<PagedResult<OrderDto>>
    {
        public OrderStatus? Status { get; set; }
        public string? RequestedByUserId { get; set; }
        public Guid? RoadAccountId { get; set; }
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
    {
        public GetOrdersQueryValidator()
        {
            RuleFor(x => x.PageNumber).InclusiveBetween(1, 10000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(200);
            RuleFor(x => x.RequestedByUserId).MaximumLength(100);
        }
    }

    public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;

        public GetOrdersQueryHandler(
            IOrderRepository orderRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext)
        {
            _orderRepository = orderRepository;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
        }

        public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            EnsureViewPermission();

            var (items, totalCount) = await _orderRepository.GetPagedAsync(
                request.Status,
                request.RequestedByUserId,
                request.RoadAccountId,
                request.Search,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return new PagedResult<OrderDto>
            {
                Items = items.Select(i => OrderDto.FromEntity(i)).ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to list orders.");
            }
        }

        private void EnsureViewPermission()
        {
            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewOrdersRoles))
            {
                throw new ForbiddenException(OmsPermissions.ViewOrders, _currentUser.UserId);
            }
        }
    }
}
