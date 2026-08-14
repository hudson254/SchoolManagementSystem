using Microsoft.EntityFrameworkCore;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories;

/// <summary>
/// Repository implementation for CertificateTemplate entity
/// </summary>
public class CertificateTemplateRepository : ICertificateTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public CertificateTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<CertificateTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateTemplate>()
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateTemplate>()
            .Where(t => t.Status == "Active")
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateTemplate>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateTemplate>()
            .Where(t => t.Type == type)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateTemplate>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateTemplate>()
            .Where(t => t.CourseId == courseId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CertificateTemplate?> GetDefaultTemplateAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateTemplate>()
            .FirstOrDefaultAsync(t => t.IsDefault && t.Status == "Active", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TemplateNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateTemplate>()
            .AnyAsync(t => t.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CertificateTemplate> AddAsync(CertificateTemplate template, CancellationToken cancellationToken = default)
    {
        await _context.Set<CertificateTemplate>().AddAsync(template, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return template;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(CertificateTemplate template, CancellationToken cancellationToken = default)
    {
        _context.Set<CertificateTemplate>().Update(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await GetByIdAsync(id, cancellationToken);
        if (template != null)
        {
            _context.Set<CertificateTemplate>().Remove(template);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<CertificateTemplate> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? type = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CertificateTemplate>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t =>
                t.Name.Contains(searchTerm) ||
                (t.Description != null && t.Description.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
