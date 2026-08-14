using SMS.Certificates.Domain.Entities;

namespace SMS.Certificates.Domain.Interfaces;

/// <summary>
/// Repository interface for CertificateAuditLog entity
/// </summary>
public interface ICertificateAuditLogRepository
{
    /// <summary>
    /// Add a new audit log entry
    /// </summary>
    Task<CertificateAuditLog> AddAsync(CertificateAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a certificate
    /// </summary>
    Task<IEnumerable<CertificateAuditLog>> GetByCertificateIdAsync(Guid certificateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by certificate number
    /// </summary>
    Task<IEnumerable<CertificateAuditLog>> GetByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by user ID
    /// </summary>
    Task<IEnumerable<CertificateAuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<IEnumerable<CertificateAuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs within a date range
    /// </summary>
    Task<IEnumerable<CertificateAuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs with pagination
    /// </summary>
    Task<(IEnumerable<CertificateAuditLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? certificateId = null,
        string? action = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
