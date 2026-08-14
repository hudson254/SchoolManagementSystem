namespace SMS.Domain.Enums
{
    /// <summary>
    /// Publication workflow status for assessment results.
    /// Draft → Pending Review → Approved → Published
    /// </summary>
    public enum ResultPublicationStatus
    {
        Draft = 1,
        PendingReview = 2,
        Approved = 3,
        Published = 4
    }
}
