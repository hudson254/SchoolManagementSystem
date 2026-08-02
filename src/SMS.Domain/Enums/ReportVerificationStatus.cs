namespace SMS.Domain.Enums
{
    /// <summary>
    /// Represents the verification status of a generated report.
    /// </summary>
    public enum ReportVerificationStatus
    {
        /// <summary>
        /// Report is valid and authentic
        /// </summary>
        Valid = 0,

        /// <summary>
        /// Report has been revoked by an administrator
        /// </summary>
        Revoked = 1,

        /// <summary>
        /// Report has expired (if applicable)
        /// </summary>
        Expired = 2,

        /// <summary>
        /// Report hash validation failed - content may have been tampered with
        /// </summary>
        Tampered = 3,

        /// <summary>
        /// Verification token not found or invalid
        /// </summary>
        Invalid = 4
    }
}
