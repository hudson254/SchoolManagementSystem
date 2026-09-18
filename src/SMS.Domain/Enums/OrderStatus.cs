namespace SMS.Domain.Enums
{
    /// <summary>
    /// Lifecycle status of an OMS order.
    /// Development default per Documentation/OMS/OMS_OPEN_REQUIREMENTS.md (#2).
    /// Persisted as int; never use magic status strings. Transitions are enforced
    /// by <see cref="SMS.Domain.Common.OrderLifecycle"/>.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>Order created, editable by its creator.</summary>
        Draft = 1,

        /// <summary>Submitted for processing (awaiting review or approval).</summary>
        Submitted = 2,

        /// <summary>Under review before final approval.</summary>
        PendingApproval = 3,

        /// <summary>Approved (terminal unless requirements change).</summary>
        Approved = 4,

        /// <summary>Rejected (terminal unless requirements change).</summary>
        Rejected = 5,

        /// <summary>Cancelled (terminal).</summary>
        Cancelled = 6
    }
}
