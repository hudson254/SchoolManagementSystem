using SMS.Certificates.Domain.Entities;

namespace SMS.Certificates.Domain.Interfaces;

/// <summary>
/// Repository interface for CertificateTemplate entity
/// </summary>
public interface ICertificateTemplateRepository
{
    /// <summary>
    /// Get template by ID
    /// </summary>
    Task<CertificateTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all templates
    /// </summary>
    Task<IEnumerable<CertificateTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active templates
    /// </summary>
    Task<IEnumerable<CertificateTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates by type
    /// </summary>
    Task<IEnumerable<CertificateTemplate>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates by course ID
    /// </summary>
    Task<IEnumerable<CertificateTemplate>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default template
    /// </summary>
    Task<CertificateTemplate?> GetDefaultTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if template name exists
    /// </summary>
    Task<bool> TemplateNameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new template
    /// </summary>
    Task<CertificateTemplate> AddAsync(CertificateTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing template
    /// </summary>
    Task UpdateAsync(CertificateTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a template
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get templates with pagination
    /// </summary>
    Task<(IEnumerable<CertificateTemplate> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? type = null,
        string? status = null,
        CancellationToken cancellationToken = default);
}
