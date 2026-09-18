using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    /// <summary>
    /// EF Core persistence for OMS CSV import batches and rows.
    /// All queries are tenant-scoped and soft-delete-aware via the application
    /// query filter configured in ApplicationDbContext.OnModelCreating.
    /// </summary>
    public class OrderImportRepository : BaseRepository<OrderImport>, IOrderImportRepository
    {
        public OrderImportRepository(ApplicationDbContext context, ILogger<OrderImportRepository> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// ID lookups must respect the tenant/soft-delete query filters; the base
        /// FindAsync implementation bypasses global query filters.
        /// </summary>
        public override async Task<OrderImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<OrderImportRow>> GetRowsAsync(
            Guid importId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<OrderImportRow>()
                .Where(r => r.ImportId == importId)
                .OrderBy(r => r.RowNumber)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OrderImport>> GetRecentAsync(
            int take,
            CancellationToken cancellationToken = default)
        {
            if (take < 1) take = 10;
            if (take > 100) take = 100;

            return await _dbSet
                .OrderByDescending(i => i.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
    }
}
