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
    public class OmsDashboardSummaryQuery : IRequest<OmsDashboardSummaryDto>
    {
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }

    public class OmsDashboardSummaryQueryHandler : IRequestHandler<OmsDashboardSummaryQuery, OmsDashboardSummaryDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;

        public OmsDashboardSummaryQueryHandler(
            IOrderRepository orderRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext)
        {
            _orderRepository = orderRepository;
            _currentUser = currentUser;
            _tenantContext = tenantContext;
        }

        public async Task<OmsDashboardSummaryDto> Handle(OmsDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            EnsureViewPermission();

            var counts = await _orderRepository.GetStatusCountsAsync(cancellationToken);
            var totalsByCurrency = await _orderRepository.GetTotalByCurrencyAsync(
                request.FromUtc, request.ToUtc, cancellationToken);

            return new OmsDashboardSummaryDto
            {
                DraftCount = counts.TryGetValue(OrderStatus.Draft, out var draft) ? draft : 0,
                SubmittedCount = counts.TryGetValue(OrderStatus.Submitted, out var submitted) ? submitted : 0,
                PendingApprovalCount = counts.TryGetValue(OrderStatus.PendingApproval, out var pending) ? pending : 0,
                ApprovedCount = counts.TryGetValue(OrderStatus.Approved, out var approved) ? approved : 0,
                RejectedCount = counts.TryGetValue(OrderStatus.Rejected, out var rejected) ? rejected : 0,
                CancelledCount = counts.TryGetValue(OrderStatus.Cancelled, out var cancelled) ? cancelled : 0,
                TotalOrders = counts.Values.Sum(),
                TotalsByCurrency = totalsByCurrency.Any()
                    ? new Dictionary<string, decimal>(totalsByCurrency)
                    : new Dictionary<string, decimal>()
            };
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                throw new UnauthorizedException("An authenticated user is required to view the OMS dashboard summary.");
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
