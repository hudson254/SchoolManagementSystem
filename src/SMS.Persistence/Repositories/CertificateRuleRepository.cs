using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories;

/// <summary>
/// Repository implementation for configurable certificate eligibility rules
/// </summary>
public class CertificateRuleRepository : BaseRepository<CertificateRule>, ICertificateRuleRepository
{
    public CertificateRuleRepository(ApplicationDbContext context, ILogger<CertificateRuleRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public async Task<CertificateRule?> GetActiveRuleAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(r => r.IsActive && !r.IsDeleted)
            .Where(r => r.EffectiveFrom == null || r.EffectiveFrom <= now)
            .Where(r => r.EffectiveTo == null || r.EffectiveTo >= now)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CertificateRule?> GetActiveRuleForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.IsActive && !r.IsDeleted)
            .Where(r => r.EffectiveFrom == null || r.EffectiveFrom <= date)
            .Where(r => r.EffectiveTo == null || r.EffectiveTo >= date)
            .OrderByDescending(r => r.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
