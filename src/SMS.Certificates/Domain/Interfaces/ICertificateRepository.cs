using SMS.Certificates.Domain.Entities;

namespace SMS.Certificates.Domain.Interfaces;

/// <summary>
/// Repository interface for Certificate entity
/// </summary>
public interface ICertificateRepository
{
    /// <summary>
    /// Get certificate by ID
    /// </summary>
    Task<Certificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get certificate by certificate number
    /// </summary>
    Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get certificate by verification token
    /// </summary>
    Task<Certificate?> GetByVerificationTokenAsync(string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all certificates for a student
    /// </summary>
    Task<IEnumerable<Certificate>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all certificates for a course offering
    /// </summary>
    Task<IEnumerable<Certificate>> GetByCourseOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if certificate number exists
    /// </summary>
    Task<bool> CertificateNumberExistsAsync(string certificateNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if verification token exists
    /// </summary>
    Task<bool> VerificationTokenExistsAsync(string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new certificate
    /// </summary>
    Task<Certificate> AddAsync(Certificate certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing certificate
    /// </summary>
    Task UpdateAsync(Certificate certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a certificate
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get certificates with pagination
    /// </summary>
    Task<(IEnumerable<Certificate> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? status = null,
        Guid? studentId = null,
        Guid? courseOfferingId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active (non-revoked, non-expired) certificates
    /// </summary>
    Task<IEnumerable<Certificate>> GetActiveCertificatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get revoked certificates
    /// </summary>
    Task<IEnumerable<Certificate>> GetRevokedCertificatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired certificates
    /// </summary>
    Task<IEnumerable<Certificate>> GetExpiredCertificatesAsync(CancellationToken cancellationToken = default);
}
