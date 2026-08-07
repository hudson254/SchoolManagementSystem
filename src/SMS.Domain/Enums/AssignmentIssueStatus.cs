namespace SMS.Domain.Enums
{
    /// <summary>
    /// Lifecycle status of an issue reported by a student or lecturer about
    /// an incorrect enrollment or teaching assignment.
    /// </summary>
    public enum AssignmentIssueStatus
    {
        Pending = 0,
        UnderReview = 1,
        Resolved = 2,
        Dismissed = 3
    }
}
