namespace SMS.Application.Common
{
    /// <summary>
    /// Defines the classification categories for errors.
    /// Used by the centralized logging pipeline and error repository
    /// to group and filter errors by domain.
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// Input validation failure.
        /// </summary>
        Validation = 0,

        /// <summary>
        /// Authentication failure (e.g. invalid credentials, expired token).
        /// </summary>
        Authentication = 1,

        /// <summary>
        /// Authorization failure (e.g. insufficient permissions).
        /// </summary>
        Authorization = 2,

        /// <summary>
        /// Business rule violation.
        /// </summary>
        BusinessRule = 3,

        /// <summary>
        /// Database operation failure.
        /// </summary>
        Database = 4,

        /// <summary>
        /// Infrastructure failure (e.g. file system, caching).
        /// </summary>
        Infrastructure = 5,

        /// <summary>
        /// Network failure.
        /// </summary>
        Network = 6,

        /// <summary>
        /// Operation timeout.
        /// </summary>
        Timeout = 7,

        /// <summary>
        /// Configuration error.
        /// </summary>
        Configuration = 8,

        /// <summary>
        /// External service failure.
        /// </summary>
        ExternalService = 9,

        /// <summary>
        /// Unclassified or unexpected error.
        /// </summary>
        Unknown = 10
    }
}
