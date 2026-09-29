using SMS.Domain.Reporting;
using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    /// <summary>A filter echoed back with the report preview / export header.</summary>
    public class ReportFilterAppliedDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Pagination metadata returned with paged report rows.</summary>
    public class ReportPaginationDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Common preview metadata for every accommodation report: title, who and
    /// when generated it, and the filters that were applied.
    /// </summary>
    public class AccommodationReportBaseDto
    {
        /// <summary>Stable report identifier used by the export endpoint.</summary>
        public string ReportKey { get; set; } = string.Empty;

        public string ReportTitle { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public string GeneratedBy { get; set; } = string.Empty;
        public List<ReportFilterAppliedDto> AppliedFilters { get; set; } = new List<ReportFilterAppliedDto>();
    }

    /// <summary>
    /// Current House Occupancy / Occupied Houses / Empty Houses report.
    /// Named distinctly from the legacy single-house <c>HouseOccupancyReportDto</c>.
    /// </summary>
    public class AccommodationHouseOccupancyReportDto : AccommodationReportBaseDto
    {
        public OccupancySummaryReportRow Summary { get; set; } = new OccupancySummaryReportRow();
        public ReportPaginationDto Pagination { get; set; } = new ReportPaginationDto();
        public List<HouseOccupancyReportRow> Rows { get; set; } = new List<HouseOccupancyReportRow>();
    }

    /// <summary>Occupancy History report for a selected date range.</summary>
    public class OccupancyHistoryReportDto : AccommodationReportBaseDto
    {
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public int DistinctOccupants { get; set; }
        public int DistinctHouses { get; set; }
        public ReportPaginationDto Pagination { get; set; } = new ReportPaginationDto();
        public List<OccupancyHistoryReportRow> Rows { get; set; } = new List<OccupancyHistoryReportRow>();
    }

    /// <summary>House History report for one selected house.</summary>
    public class HouseOccupancyHistoryReportDto : AccommodationReportBaseDto
    {
        public Guid HouseId { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string? HouseName { get; set; }
        public string LaneName { get; set; } = string.Empty;
        public string HouseStatus { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int CurrentOccupants { get; set; }
        public int TotalStays { get; set; }
        public ReportPaginationDto Pagination { get; set; } = new ReportPaginationDto();
        public List<OccupancyHistoryReportRow> Rows { get; set; } = new List<OccupancyHistoryReportRow>();
    }

    /// <summary>Occupancy by Period report (summary + per-house breakdown).</summary>
    public class OccupancyByPeriodReportDto : AccommodationReportBaseDto
    {
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public string PeriodLabel { get; set; } = "All time";
        public OccupancySummaryReportRow Summary { get; set; } = new OccupancySummaryReportRow();
        public ReportPaginationDto Pagination { get; set; } = new ReportPaginationDto();
        public List<OccupancyByPeriodHouseRow> Rows { get; set; } = new List<OccupancyByPeriodHouseRow>();
    }

    /// <summary>
    /// Occupant Accommodation History: search mode returns matching occupants;
    /// detail mode returns the selected occupant's complete stay history.
    /// </summary>
    public class OccupantAccommodationHistoryReportDto : AccommodationReportBaseDto
    {
        /// <summary>"Search" or "Detail".</summary>
        public string Mode { get; set; } = "Search";

        public OccupantCandidateRow? SelectedOccupant { get; set; }
        public List<OccupantCandidateRow> Candidates { get; set; } = new List<OccupantCandidateRow>();
        public List<OccupancyHistoryReportRow> Stays { get; set; } = new List<OccupancyHistoryReportRow>();
        public string CurrentHouse { get; set; } = string.Empty;
        public ReportPaginationDto Pagination { get; set; } = new ReportPaginationDto();
    }

    /// <summary>House Utilization Summary report.</summary>
    public class HouseUtilizationSummaryReportDto : AccommodationReportBaseDto
    {
        public OccupancySummaryReportRow Summary { get; set; } = new OccupancySummaryReportRow();
    }
}
