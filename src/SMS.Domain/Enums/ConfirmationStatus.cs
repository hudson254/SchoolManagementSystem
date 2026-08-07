namespace SMS.Domain.Enums
{
    /// <summary>
    /// Confirmation status for a student enrollment or lecturer teaching
    /// assignment against a course offering.
    /// </summary>
    public enum ConfirmationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Reported = 2,
        Resolved = 3
    }
}
