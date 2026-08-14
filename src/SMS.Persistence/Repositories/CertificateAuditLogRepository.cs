using Microsoft.EntityFrameworkCore;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories;

/// <summary>
/// Repository implementation for CertificateAuditLog entity
/// </summary>
public class CertificateAuditLogRepository : ICertificateAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public CertificateAuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<CertificateAuditLog> AddAsync(CertificateAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.Set<CertificateAuditLog>().AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateAuditLog>> GetByCertificateIdAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateAuditLog>()
            .Where(l => l.CertificateId == certificateId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateAuditLog>> GetByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateAuditLog>()
            .Where(l => l.CertificateNumber == certificateNumber)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateAuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateAuditLog>()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateAuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateAuditLog>()
            .Where(l => l.Action == action)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CertificateAuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<CertificateAuditLog>()
            .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<CertificateAuditLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? certificateId = null,
        string? action = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CertificateAuditLog>().AsQueryable();

        if (certificateId.HasValue)
        {
            query = query.Where(l => l.CertificateId == certificateId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(l => l.Action == action);
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
