using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Reporting
{
    /// <summary>
    /// One house row for the current / occupied / empty house reports.
    /// </summary>
    public class HouseOccupancyReportRow
    {
        public Guid HouseId { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string? HouseName { get; set; }
        public Guid LaneId { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Capacity { get; set; }

        /// <summary>Occupied count derived from active occupancy records (authoritative).</summary>
        public int OccupiedCount { get; set; }

        public int AvailableSpaces { get; set; }
        public bool IsOccupied { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsEnabled { get; set; }

        /// <summary>Occupied, Empty or Full (at capacity).</summary>
        public string OccupancyStatus { get; set; } = "Empty";

        public List<HouseOccupantReportRow> CurrentOccupants { get; set; } = new List<HouseOccupantReportRow>();

        // ===== Populated by the Empty Houses report (historical data) =====
        public string? LastOccupantName { get; set; }
        public string? LastOccupantNumber { get; set; }
        public OccupantType? LastOccupantType { get; set; }
        public DateTime? LastOccupancyEndDate { get; set; }

        /// <summary>Total occupancy records the house has ever had (0 = never occupied).</summary>
        public int HistoricalOccupantCount { get; set; }

        public DateTime? OccupiedDate { get; set; }
        public DateTime? VacatedDate { get; set; }
    }

    /// <summary>
    /// One active occupant inside a house row.
    /// </summary>
    public class HouseOccupantReportRow
    {
        public Guid AssignmentId { get; set; }
        public Guid OccupantId { get; set; }
        public OccupantType OccupantType { get; set; }
        public string OccupantName { get; set; } = string.Empty;

        /// <summary>Student number or employee number.</summary>
        public string? OccupantNumber { get; set; }

        /// <summary>Date the occupant was allocated to the house.</summary>
        public DateTime AllocationDate { get; set; }

        public DateTime? MoveInDate { get; set; }
        public DateTime? CheckInDate { get; set; }
        public string AssignmentStatus { get; set; } = "Active";
        public Guid? SemesterId { get; set; }
        public string? SemesterName { get; set; }
    }
}
