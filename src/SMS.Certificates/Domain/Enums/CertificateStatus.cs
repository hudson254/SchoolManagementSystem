namespace SMS.Certificates.Domain.Enums;

/// <summary>
/// Represents the current status of a certificate
/// </summary>
public enum CertificateStatus
{
    /// <summary>
    /// Certificate has been issued and is valid
    /// </summary>
    Issued = 1,

    /// <summary>
    /// Certificate has been revoked and is no longer valid
    /// </summary>
    Revoked = 2,

    /// <summary>
    /// Certificate has expired
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Certificate has been superseded by a newer version
    /// </summary>
    Superseded = 4,

    /// <summary>
    /// Certificate is pending issuance
    /// </summary>
    Pending = 5
}
