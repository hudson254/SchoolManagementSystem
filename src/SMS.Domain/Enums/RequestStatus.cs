namespace SMS.Domain.Enums
{
    /// <summary>
    /// Lifecycle status of an SMS request.
    /// </summary>
    public enum RequestStatus
    {
        Draft = 1,
        Submitted = 2,
        PendingReview = 3,
        Assigned = 4,
        PendingApproval = 5,
        Approved = 6,
        Rejected = 7,
        Returned = 8,
        InProgress = 9,
        Completed = 10,
        Cancelled = 11,
        OnHold = 12,
        Escalated = 13
    }
}
