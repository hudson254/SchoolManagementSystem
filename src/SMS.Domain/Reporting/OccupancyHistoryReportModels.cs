using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Reporting
{
    /// <summary>
    /// One occupancy record (house + occupant + occupancy interval) used by the
    /// occupancy history, house history and occupant history reports.
    /// </summary>
    public class OccupancyHistoryReportRow
    {
        public Guid AssignmentId { get; set; }
        public Guid HouseId { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string? HouseName { get; set; }
        public string LaneName { get; set; } = string.Empty;

        public Guid OccupantId { get; set; }
        public OccupantType OccupantType { get; set; }
        public string OccupantName { get; set; } = string.Empty;
        public string? OccupantNumber { get; set; }

        /// <summary>Occupancy start (move-in when present, otherwise the allocation date).</summary>
        public DateTime OccupancyStartDate { get; set; }

        /// <summary>Occupancy end; null while the occupant is still in the house.</summary>
        public DateTime? OccupancyEndDate { get; set; }

        /// <summary>True while this occupancy is the occupant's current one.</summary>
        public bool IsCurrent { get; set; }

        /// <summary>Whole days the occupant stayed (to date for current stays).</summary>
        public int DurationDays { get; set; }

        public string Status { get; set; } = string.Empty;
        public Guid? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public string? AcademicYearName { get; set; }
    }

    /// <summary>
    /// One occupant matching a search in the occupant history report.
    /// </summary>
    public class OccupantCandidateRow
    {
        public Guid OccupantId { get; set; }
        public OccupantType OccupantType { get; set; }
        public string OccupantName { get; set; } = string.Empty;
        public string? OccupantNumber { get; set; }
        public Guid? CurrentHouseId { get; set; }
        public string? CurrentHouseNumber { get; set; }
        public int TotalStays { get; set; }
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// One house breakdown row for the occupancy-by-period report.
    /// </summary>
    public class OccupancyByPeriodHouseRow
    {
        public Guid HouseId { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string? HouseName { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int OccupiedInPeriod { get; set; }
        public int AvailableInPeriod { get; set; }
        public string OccupantsInPeriod { get; set; } = string.Empty;
        public bool WasOccupiedInPeriod => OccupiedInPeriod > 0;
    }
}
