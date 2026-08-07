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
        Cancelled = 4
    }
}
