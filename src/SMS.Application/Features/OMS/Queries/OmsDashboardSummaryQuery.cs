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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Queries
{
        /// <summary>Tenant-scoped OMS dashboard summary for Orders (status counters + totals).</summary>
    public class OmsDashboardSummaryQuery : IRequest<OmsDashboardSummaryDto>
    {
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }

    public class OmsDashboardSummaryQueryHandler : IRequestHandler<OmsDashboardSummaryQuery, OmsDashboardSummaryDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

        public OmsDashboardSummaryQueryHandler(
            IOrderRepository orderRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _orderRepository = orderRepository;
            _currentUser = currentUser;
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

    public class GetRequestDashboardSummaryQuery : IRequest<RequestDashboardSummaryDto>
    {
    }

    public class GetRequestDashboardSummaryQueryHandler : IRequestHandler<GetRequestDashboardSummaryQuery, RequestDashboardSummaryDto>
    {
        private readonly IRequestRepository _requestRepository;
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUser;

        public GetRequestDashboardSummaryQueryHandler(
            IRequestRepository requestRepository,
            SMS.Application.Common.Interfaces.ICurrentUserService currentUser)
        {
            _requestRepository = requestRepository;
            _currentUser = currentUser;
        }

        public async Task<RequestDashboardSummaryDto> Handle(GetRequestDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedException("An authenticated user is required to view the request dashboard summary.");

            if (!OmsAuthorization.HasAnyRole(_currentUser.Roles, OmsAuthorization.ViewRequestsRoles))
                throw new ForbiddenException(OmsPermissions.ViewRequests, _currentUser.UserId);

            var counts = await _requestRepository.GetStatusCountsAsync(cancellationToken);
            return new RequestDashboardSummaryDto
            {
                TotalRequests = counts.Values.Sum(),
                DraftCount = counts.TryGetValue(RequestStatus.Draft, out var draft) ? draft : 0,
                SubmittedCount = counts.TryGetValue(RequestStatus.Submitted, out var submitted) ? submitted : 0,
                PendingReviewCount = counts.TryGetValue(RequestStatus.PendingReview, out var pendingReview) ? pendingReview : 0,
                AssignedCount = counts.TryGetValue(RequestStatus.Assigned, out var assigned) ? assigned : 0,
                PendingApprovalCount = counts.TryGetValue(RequestStatus.PendingApproval, out var pendingApproval) ? pendingApproval : 0,
                ApprovedCount = counts.TryGetValue(RequestStatus.Approved, out var approved) ? approved : 0,
                RejectedCount = counts.TryGetValue(RequestStatus.Rejected, out var rejected) ? rejected : 0,
                ReturnedCount = counts.TryGetValue(RequestStatus.Returned, out var returned) ? returned : 0,
                InProgressCount = counts.TryGetValue(RequestStatus.InProgress, out var inProgress) ? inProgress : 0,
                CompletedCount = counts.TryGetValue(RequestStatus.Completed, out var completed) ? completed : 0,
                CancelledCount = counts.TryGetValue(RequestStatus.Cancelled, out var cancelled) ? cancelled : 0,
                OnHoldCount = counts.TryGetValue(RequestStatus.OnHold, out var onHold) ? onHold : 0,
                EscalatedCount = counts.TryGetValue(RequestStatus.Escalated, out var escalated) ? escalated : 0,
                CountsByStatus = counts
            };
        }
    }
}
