using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// OMS order repository. Extends the generic repository contract with the
    /// OMS-specific read shapes (eager-loaded aggregates, append-only history,
    /// attachments/manifests, and paged listing) so the Application layer never
    /// reaches into <c>DbContext</c> directly.
    /// </summary>
    public interface IOrderRepository : IRepository<Order>
    {
        /// <summary>Loads an order with items + status history. Returns null when not found for the current tenant.</summary>
        Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Finds an order by its immutable order number. Returns null when not found for the current tenant.</summary>
        Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

        /// <summary>Append-only status history for an order, oldest first.</summary>
        Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>Attachments linked to an order, newest first.</summary>
        Task<IReadOnlyList<OrderAttachment>> GetAttachmentsAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>Generated manifests for an order, newest first.</summary>
        Task<IReadOnlyList<OrderManifest>> GetManifestsAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paged, tenant-scoped order listing with the OMS filter set applied
        /// server-side. Returns the page of orders and the total matching count.
        /// </summary>
        Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
            SMS.Domain.Enums.OrderStatus? status,
            string? requestedByUserId,
            Guid? roadAccountId,
            string? search,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>Counts orders grouped by status for the current tenant (OMS reporting).</summary>
        Task<IReadOnlyDictionary<SMS.Domain.Enums.OrderStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sums order totals grouped by currency for the current tenant over an
        /// optional date window (OMS reporting). Monetary values stay decimal.
        /// </summary>
        Task<IReadOnlyDictionary<string, decimal>> GetTotalByCurrencyAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default);
    }
}