namespace SMS.Domain.Enums
{
    /// <summary>
    /// Moderation workflow status for assessment marks.
    /// </summary>
    public enum ModerationStatus
    {
        NotRequired = 1,
        PendingReview = 2,
        ReturnedForCorrection = 3,
        Approved = 4
    }
}
