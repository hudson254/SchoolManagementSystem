using SMS.Domain.Enums;
using SMS.Domain.Reporting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Read-model repository backing the Accommodation reports.
    ///
    /// Implementations MUST stay database-side: projection, filtering,
    /// aggregation and pagination happen in PostgreSQL so reports remain usable
    /// with thousands of occupants and occupancy records. Results are tenant
    /// isolated through the shared DbContext query filters and PostgreSQL RLS.
    /// </summary>
    public interface IAccommodationReportRepository
    {
        /// <summary>
        /// Houses for the current occupancy / occupied houses / empty houses reports
        /// plus the aggregate summary for the same filters (single lightweight
        /// aggregation query, no N+1).
        /// </summary>
        Task<(ReportPage<HouseOccupancyReportRow> Page, OccupancySummaryReportRow Summary)> GetHouseOccupancyReportAsync(
            AccommodationReportFilters filters,
            HouseOccupancyScope scope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Occupancy records that overlap the reporting period using proper
        /// date-range overlap logic (start &lt;= periodEnd AND (end IS NULL OR
        /// end &gt;= periodStart)).
        /// </summary>
        Task<(ReportPage<OccupancyHistoryReportRow> Page, int DistinctOccupants, int DistinctHouses)> GetOccupancyHistoryReportAsync(
            AccommodationReportFilters filters,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Complete occupancy history of one house, oldest first, together with
        /// the number of occupants currently active in that house.
        /// </summary>
        Task<(ReportPage<OccupancyHistoryReportRow> Page, int CurrentOccupants)> GetHouseHistoryReportAsync(
            Guid houseId,
            AccommodationReportFilters filters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Occupancy-by-period aggregates plus a per-house breakdown page.
        /// </summary>
        Task<(ReportPage<OccupancyByPeriodHouseRow> Rows, OccupancySummaryReportRow Summary)> GetOccupancyByPeriodReportAsync(
            AccommodationReportFilters filters,
            DateTime? periodStart,
            DateTime? periodEnd,
            CancellationToken cancellationToken = default);

        /// <summary>House utilization summary for the given filters.</summary>
        Task<OccupancySummaryReportRow> GetUtilizationSummaryAsync(
            AccommodationReportFilters filters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Occupants matching a search term (students and/or lecturers) with their
        /// current house. Bounded by <see cref="AccommodationReportFilters.PageSize"/>.
        /// </summary>
        Task<List<OccupantCandidateRow>> SearchOccupantsAsync(
            AccommodationReportFilters filters,
            CancellationToken cancellationToken = default);

        /// <summary>Resolves one occupant (student or lecturer) for the occupant history report.</summary>
        Task<OccupantCandidateRow?> GetOccupantAsync(
            Guid occupantId,
            OccupantType occupantType,
            CancellationToken cancellationToken = default);

        /// <summary>All occupancy records of one occupant, oldest first.</summary>
        Task<List<OccupancyHistoryReportRow>> GetOccupantStaysAsync(
            Guid occupantId,
            OccupantType occupantType,
            CancellationToken cancellationToken = default);
    }
}
