using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Common
{
    /// <summary>
    /// SMS request state machine.
    /// Transitions are enforced here - never assign Request.Status directly.
    /// </summary>
    public static class RequestLifecycle
    {
        /// <summary>Valid transitions by source status.</summary>
        public static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> AllowedTransitions =
            new Dictionary<RequestStatus, RequestStatus[]>
            {
                [RequestStatus.Draft] = new[] { RequestStatus.Submitted, RequestStatus.Cancelled },
                [RequestStatus.Submitted] = new[] { RequestStatus.PendingReview, RequestStatus.Assigned, RequestStatus.Rejected, RequestStatus.Cancelled, RequestStatus.OnHold },
                [RequestStatus.PendingReview] = new[] { RequestStatus.Assigned, RequestStatus.Returned, RequestStatus.Rejected, RequestStatus.Cancelled },
                [RequestStatus.Assigned] = new[] { RequestStatus.PendingApproval, RequestStatus.InProgress, RequestStatus.Returned, RequestStatus.Rejected, RequestStatus.Cancelled, RequestStatus.OnHold, RequestStatus.Escalated },
                [RequestStatus.PendingApproval] = new[] { RequestStatus.Approved, RequestStatus.Rejected, RequestStatus.Returned, RequestStatus.Cancelled },
                [RequestStatus.Approved] = new[] { RequestStatus.InProgress, RequestStatus.Completed, RequestStatus.Cancelled },
                [RequestStatus.InProgress] = new[] { RequestStatus.Completed, RequestStatus.OnHold, RequestStatus.Escalated, RequestStatus.Cancelled },
                [RequestStatus.Returned] = new[] { RequestStatus.Draft, RequestStatus.Submitted, RequestStatus.Cancelled },
                [RequestStatus.OnHold] = new[] { RequestStatus.Assigned, RequestStatus.InProgress, RequestStatus.Cancelled },
                [RequestStatus.Escalated] = new[] { RequestStatus.PendingApproval, RequestStatus.Assigned, RequestStatus.Cancelled },
                [RequestStatus.Completed] = Array.Empty<RequestStatus>(),
                [RequestStatus.Rejected] = Array.Empty<RequestStatus>(),
                [RequestStatus.Cancelled] = Array.Empty<RequestStatus>()
            };

        public static bool CanTransition(RequestStatus from, RequestStatus to)
        {
            if (from == to) return false;
            return AllowedTransitions.TryGetValue(from, out var targets) && Array.IndexOf(targets, to) >= 0;
        }

        public static void EnsureCanTransition(RequestStatus from, RequestStatus to)
        {
            if (!CanTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Invalid request status transition: {from} → {to}. Allowed targets from {from}: " +
                    (AllowedTransitions.TryGetValue(from, out var targets) && targets.Length > 0
                        ? string.Join(", ", targets)
                        : "none (terminal status)"));
            }
        }

        public static bool IsTerminal(RequestStatus status) =>
            status is RequestStatus.Completed or RequestStatus.Rejected or RequestStatus.Cancelled;

        public static bool CanEdit(RequestStatus status) => status == RequestStatus.Draft || status == RequestStatus.Returned;
    }
}
