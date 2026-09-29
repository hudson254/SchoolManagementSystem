using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Reporting
{
    /// <summary>
    /// Which slice of houses an occupancy report covers.
    /// </summary>
    public enum HouseOccupancyScope
    {
        /// <summary>All houses.</summary>
        All = 0,
        /// <summary>Only houses with at least one active occupant.</summary>
        Occupied = 1,
        /// <summary>Only houses with no active occupants.</summary>
        Empty = 2
    }

    /// <summary>
    /// Common, optional filters shared by the accommodation report queries.
    /// Every property is optional; reports never require unnecessary fields.
    /// Dates are application-UTC (the project stores all occupancy timestamps in UTC).
    /// </summary>
    public class AccommodationReportFilters
    {
        /// <summary>Restrict to a single lane.</summary>
        public Guid? LaneId { get; set; }

        /// <summary>Restrict to a single house.</summary>
        public Guid? HouseId { get; set; }

        /// <summary>House status filter (Vacant, Occupied, Maintenance, ...).</summary>
        public string? Status { get; set; }

        /// <summary>Restrict occupants of a given type (Student / Lecturer).</summary>
        public OccupantType? OccupantType { get; set; }

        /// <summary>Restrict to an academic period (semester).</summary>
        public Guid? SemesterId { get; set; }

        /// <summary>Restrict to an academic year (via the assignment's semester).</summary>
        public Guid? AcademicYearId { get; set; }

        /// <summary>Inclusive start of the reporting period.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Inclusive end of the reporting period.</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>Free-text search (occupant name/number, house number/name, lane).</summary>
        public string? SearchTerm { get; set; }

        /// <summary>1-based page number.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Page size (clamped by the caller).</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// A single page of report rows plus paging metadata.
    /// </summary>
    public class ReportPage<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }

    /// <summary>
    /// Aggregated occupancy metrics shared by the report summaries.
    /// </summary>
    public class OccupancySummaryReportRow
    {
        public int TotalHouses { get; set; }
        public int OccupiedHouses { get; set; }
        public int EmptyHouses { get; set; }
        public int TotalCapacity { get; set; }
        public int OccupiedSpaces { get; set; }
        public int AvailableSpaces { get; set; }
        public int HousesAtFullCapacity { get; set; }
        public int HousesWithAvailableCapacity { get; set; }
        public int HousesNeverOccupied { get; set; }
        public decimal OccupancyPercentage { get; set; }
    }
}
