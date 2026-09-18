using System;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Road Accounting service boundary.
    ///
    /// Phase 2 provides persistence + this abstraction ONLY: the actual
    /// allocation/posting formulas are an open requirement
    /// (Documentation/OMS/OMS_OPEN_REQUIREMENTS.md #18) and MUST NOT be invented.
    /// Implementations return <see cref="Common.RoadAccountingResult.NotConfigured"/>
    /// until confirmed rules are supplied.
    /// </summary>
    public interface IRoadAccountingService
    {
        /// <summary>
        /// Computes the road-account allocation for an order.
        /// Returns <see cref="Common.RoadAccountingResult.NotConfigured"/> while
        /// the business rules are unknown.
        /// </summary>
        Task<Common.RoadAccountingResult> ComputeOrderAllocationAsync(Order order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Posts an order to its road account.
        /// Returns <see cref="Common.RoadAccountingResult.NotConfigured"/> while
        /// the business rules are unknown. Must not mutate balances.
        /// </summary>
        Task<Common.RoadAccountingResult> PostOrderToAccountAsync(Order order, CancellationToken cancellationToken = default);
    }
}
