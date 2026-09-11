namespace SMS.Domain.Enums
{
    /// <summary>
    /// Lifecycle status of a course offering.
    /// </summary>
    public enum CourseOfferingStatus
    {
        Draft = 0,
        Active = 1,
        Completed = 2,
        Closed = 3,
        Cancelled = 4,
        // Added after Cancelled so persisted ordinal values are NOT renumbered.
        // The course-offering UI/dashboard treat this as an expected lifecycle
        // state (e.g. upcoming offerings).
        Scheduled = 5
    }
}
