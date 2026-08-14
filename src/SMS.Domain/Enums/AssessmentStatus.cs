namespace SMS.Domain.Enums
{
    /// <summary>
    /// Lifecycle status of an assessment.
    /// </summary>
    public enum AssessmentStatus
    {
        Draft = 1,
        Active = 2,
        GradingInProgress = 3,
        Graded = 4,
        Published = 5,
        Closed = 6,
        Archived = 7
    }
}
