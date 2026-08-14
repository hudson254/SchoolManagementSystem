using Microsoft.EntityFrameworkCore;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Enums;
using SMS.Certificates.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories;

/// <summary>
/// Repository implementation for Certificate entity
/// </summary>
public class CertificateRepository : ICertificateRepository
{
    private readonly ApplicationDbContext _context;

    public CertificateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Certificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .Include(c => c.Template)
            .Include(c => c.ParentCertificate)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Certificate?> GetByVerificationTokenAsync(string verificationToken, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .Include(c => c.Template)
            .FirstOrDefaultAsync(c => c.VerificationToken == verificationToken, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetByCourseOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .Where(c => c.CourseOfferingId == courseOfferingId)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CertificateNumberExistsAsync(string certificateNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .AnyAsync(c => c.CertificateNumber == certificateNumber, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> VerificationTokenExistsAsync(string verificationToken, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .AnyAsync(c => c.VerificationToken == verificationToken, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Certificate> AddAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        await _context.Set<Certificate>().AddAsync(certificate, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return certificate;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        _context.Set<Certificate>().Update(certificate);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var certificate = await GetByIdAsync(id, cancellationToken);
        if (certificate != null)
        {
            _context.Set<Certificate>().Remove(certificate);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Certificate> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? status = null,
        Guid? studentId = null,
        Guid? courseOfferingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Certificate>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c =>
                c.CertificateNumber.Contains(searchTerm) ||
                c.StudentId.ToString().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status.ToString() == status);
        }

        if (studentId.HasValue)
        {
            query = query.Where(c => c.StudentId == studentId.Value);
        }

        if (courseOfferingId.HasValue)
        {
            query = query.Where(c => c.CourseOfferingId == courseOfferingId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.IssueDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetActiveCertificatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .Where(c => c.Status == CertificateStatus.Issued)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetRevokedCertificatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Certificate>()
            .Where(c => c.Status == CertificateStatus.Revoked)
            .OrderByDescending(c => c.RevokedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Certificate>> GetExpiredCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<Certificate>()
            .Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < now)
            .OrderByDescending(c => c.ExpiryDate)
            .ToListAsync(cancellationToken);
    }
}
