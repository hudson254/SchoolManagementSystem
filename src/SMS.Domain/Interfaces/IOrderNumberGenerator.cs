using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Generates tenant-unique, immutable OMS order numbers.
    /// Development default format (OMS open requirement #1):
    /// <c>ORD-&lt;yyyy&gt;-&lt;6-digit per-tenant sequence&gt;</c> e.g. ORD-2026-000001.
    /// Implementations must be safe under concurrent order creation and must not
    /// derive numbers from MAX(existing) scans. The strategy is isolated behind
    /// this interface so it can be changed without touching the Order entity.
    /// </summary>
    public interface IOrderNumberGenerator
    {
        /// <summary>
        /// Returns the next order number for the given tenant.
        /// Must be called before the order is persisted so the number can be
        /// written with the order in the same transaction.
        /// </summary>
        Task<string> GenerateNextOrderNumberAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }
}
