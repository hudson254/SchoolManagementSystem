using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Road Accounting repository. Phase 2 provides persistence only — the
    /// allocation/posting formulas remain an open requirement
    /// (Documentation/OMS/OMS_OPEN_REQUIREMENTS.md #18) and are not invented here.
    /// </summary>
    public interface IRoadAccountRepository : IRepository<RoadAccount>
    {
        /// <summary>Finds a road account by its tenant-unique code. Null when absent.</summary>
        Task<RoadAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>Active road accounts for the current tenant, ordered by code.</summary>
        Task<IReadOnlyList<RoadAccount>> GetActiveAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// CSV import repository. Imports are staged (rows recorded with per-row
    /// validation outcomes) so an import can be reviewed before it is applied
    /// to orders.
    /// </summary>
    public interface IOrderImportRepository : IRepository<OrderImport>
    {
        /// <summary>Rows belonging to an import batch, in source order.</summary>
        Task<IReadOnlyList<OrderImportRow>> GetRowsAsync(Guid importId, CancellationToken cancellationToken = default);

        /// <summary>Most recent import batches for the current tenant, newest first.</summary>
        Task<IReadOnlyList<OrderImport>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
    }
}