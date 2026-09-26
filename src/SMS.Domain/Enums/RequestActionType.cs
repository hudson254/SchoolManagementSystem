namespace SMS.Domain.Enums
{
    /// <summary>
    /// The kind of action that produced a RequestStatusHistory entry.
    /// </summary>
    public enum RequestActionType
    {
        Created = 1,
        Submitted = 2,
        ReviewStarted = 3,
        Assigned = 4,
        Reassigned = 5,
        Approved = 6,
        Rejected = 7,
        Returned = 8,
        Completed = 9,
        Cancelled = 10,
        Escalated = 11,
        Edited = 12,
        InProgress = 13,
        OnHold = 14
    }
}
