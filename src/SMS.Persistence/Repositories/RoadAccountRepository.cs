using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    /// <summary>
    /// EF Core persistence for OMS road accounts. All queries are tenant-scoped
    /// and soft-delete-aware via the application query filter configured in
    /// ApplicationDbContext.OnModelCreating.
    /// </summary>
    public class RoadAccountRepository : BaseRepository<RoadAccount>, IRoadAccountRepository
    {
        public RoadAccountRepository(ApplicationDbContext context, ILogger<RoadAccountRepository> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// ID lookups must respect the tenant/soft-delete query filters; the base
        /// FindAsync implementation bypasses global query filters.
        /// </summary>
        public override async Task<RoadAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<RoadAccount?> GetByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
        }

        public async Task<IReadOnlyList<RoadAccount>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.IsActive)
                .OrderBy(a => a.Code)
                .ToListAsync(cancellationToken);
        }
    }
}
