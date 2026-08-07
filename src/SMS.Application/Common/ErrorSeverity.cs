namespace SMS.Application.Common
{
    /// <summary>
    /// Defines the severity levels for error classification.
    /// Used by the centralized logging pipeline and error repository.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Informational message - no action required.
        /// </summary>
        Information = 0,

        /// <summary>
        /// Low severity - minor issue, no user impact.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium severity - partial impact, requires attention.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High severity - significant impact, requires prompt attention.
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical severity - system outage or data loss, requires immediate attention.
        /// </summary>
        Critical = 4
    }
}
