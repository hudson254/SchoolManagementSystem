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
    /// EF Core persistence for OMS orders. All queries are tenant-scoped and
    /// soft-delete-aware via the application query filter configured in
    /// ApplicationDbContext.OnModelCreating.
    /// </summary>
    public class OrderRepository : BaseRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context, ILogger<OrderRepository> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// ID lookups must respect the tenant/soft-delete query filters; the base
        /// FindAsync implementation bypasses global query filters.
        /// </summary>
        public override async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        /// <summary>
        /// Updates must not mark the whole object graph as modified: newly added
        /// child entities (items, status history) carry client-generated keys and
        /// would otherwise be flagged Modified before existing in the store.
        /// Attached entities are already change-tracked, so SaveChanges persists them.
        /// </summary>
        public override Task UpdateAsync(Order entity, CancellationToken cancellationToken = default)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Update(entity);
            }

            return Task.CompletedTask;
        }

        public async Task<Order?> GetByIdWithDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .Include(o => o.Attachments)
                .Include(o => o.Manifests)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetByOrderNumberAsync(
            string orderNumber,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        }

        public async Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<OrderStatusHistory>()
                .Where(h => h.OrderId == orderId)
                .OrderBy(h => h.PerformedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OrderAttachment>> GetAttachmentsAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<OrderAttachment>()
                .Where(a => a.OrderId == orderId)
                .OrderByDescending(a => a.UploadedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OrderManifest>> GetManifestsAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<OrderManifest>()
                .Where(m => m.OrderId == orderId)
                .OrderByDescending(m => m.GeneratedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
            OrderStatus? status,
            string? requestedByUserId,
            Guid? roadAccountId,
            string? search,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o =>
                    o.OrderNumber.Contains(term) ||
                    o.Title.Contains(term) ||
                    (o.Description != null && o.Description.Contains(term)));
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(requestedByUserId))
            {
                query = query.Where(o => o.RequestedByUserId == requestedByUserId);
            }

            if (roadAccountId.HasValue)
            {
                query = query.Where(o => o.RoadAccountId == roadAccountId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items.AsReadOnly(), totalCount);
        }

        public async Task<IReadOnlyDictionary<OrderStatus, int>> GetStatusCountsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetTotalByCurrencyAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (fromUtc.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= toUtc.Value);
            }

            return await query
                .GroupBy(o => o.Currency)
                .Select(g => new { Currency = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToDictionaryAsync(x => x.Currency, x => x.Total, cancellationToken);
        }
    }
}
