using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Common
{
    /// <summary>
    /// OMS order state machine (development default per
    /// Documentation/OMS/OMS_OPEN_REQUIREMENTS.md #2, #6, #11-#13).
    /// Transitions are enforced here — never assign Order.Status directly.
    /// The review step is optional (Submitted → Approved/Rejected is allowed).
    /// Approved/Rejected/Cancelled are terminal; no reopen.
    /// </summary>
    public static class OrderLifecycle
    {
        /// <summary>Valid transitions by source status.</summary>
        public static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> AllowedTransitions =
            new Dictionary<OrderStatus, OrderStatus[]>
            {
                [OrderStatus.Draft] = new[] { OrderStatus.Submitted, OrderStatus.Cancelled },
                [OrderStatus.Submitted] = new[] { OrderStatus.PendingApproval, OrderStatus.Approved, OrderStatus.Rejected, OrderStatus.Cancelled },
                [OrderStatus.PendingApproval] = new[] { OrderStatus.Approved, OrderStatus.Rejected, OrderStatus.Cancelled },
                [OrderStatus.Approved] = Array.Empty<OrderStatus>(),
                [OrderStatus.Rejected] = Array.Empty<OrderStatus>(),
                [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
            };

        /// <summary>Returns whether moving from one status to another is allowed.</summary>
        public static bool CanTransition(OrderStatus from, OrderStatus to)
        {
            if (from == to)
            {
                return false;
            }

            return AllowedTransitions.TryGetValue(from, out var targets)
                && Array.IndexOf(targets, to) >= 0;
        }

        /// <summary>Throws when the transition is not allowed (including no-op).</summary>
        public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
        {
            if (!CanTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Invalid order status transition: {from} → {to}. Allowed targets from {from}: " +
                    (AllowedTransitions.TryGetValue(from, out var targets) && targets.Length > 0
                        ? string.Join(", ", targets)
                        : "none (terminal status)"));
            }
        }

        /// <summary>Terminal statuses accept no further transitions.</summary>
        public static bool IsTerminal(OrderStatus status) =>
            status is OrderStatus.Approved or OrderStatus.Rejected or OrderStatus.Cancelled;

        /// <summary>Only Draft orders may have their contents edited.</summary>
        public static bool CanEdit(OrderStatus status) => status == OrderStatus.Draft;
    }
}
