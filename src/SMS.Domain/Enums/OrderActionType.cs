namespace SMS.Domain.Enums
{
    /// <summary>
    /// The kind of action that produced an <see cref="Entities.OrderStatusHistory"/> entry.
    /// </summary>
    public enum OrderActionType
    {
        /// <summary>Order created (Draft).</summary>
        Created = 1,

        /// <summary>Order submitted by its creator.</summary>
        Submitted = 2,

        /// <summary>Order moved into review (PendingApproval).</summary>
        ReviewStarted = 3,

        /// <summary>Order approved.</summary>
        Approved = 4,

        /// <summary>Order rejected.</summary>
        Rejected = 5,

        /// <summary>Order cancelled.</summary>
        Cancelled = 6,

        /// <summary>Order contents edited while in Draft.</summary>
        Edited = 7
    }
}
