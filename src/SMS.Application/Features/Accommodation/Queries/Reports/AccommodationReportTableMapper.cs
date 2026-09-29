using SMS.Application.DTOs;
using SMS.Domain.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// Maps report preview DTOs to display-ready <see cref="ReportTableDocument"/>
    /// instances. Both the PDF (header + filters + summary + table) and the Excel
    /// export are rendered from the same projection, so an exported file always
    /// matches the preview for the same filters.
    /// </summary>
    internal static class AccommodationReportTableMapper
    {
        private const string DateFormat = "yyyy-MM-dd";

        public static ReportTableDocument FromHouseOccupancy(AccommodationHouseOccupancyReportDto dto)
        {
            var document = Base(dto);
            document.SummaryLines = HouseOccupancySummaryLines(dto.Summary);
            document.Columns = new List<string>
            {
                "Lane", "House No", "House Name", "House Status", "Occupancy", "Capacity",
                "Occupants", "Available", "Current Occupants"
            };

            document.Rows = dto.Rows.Select(r => new List<string>
            {
                r.LaneName,
                r.HouseNumber,
                r.HouseName ?? string.Empty,
                r.Status,
                r.OccupancyStatus,
                r.Capacity.ToString(),
                r.OccupiedCount.ToString(),
                r.AvailableSpaces.ToString(),
                string.Join(", ", r.CurrentOccupants.Select(o => o.OccupantNumber != null
                    ? $"{o.OccupantName} ({o.OccupantNumber})"
                    : o.OccupantName))
            }).ToList();

            return document;
        }

        public static ReportTableDocument FromOccupancyHistory(OccupancyHistoryReportDto dto)
        {
            var document = Base(dto);
            document.SummaryLines = new List<string>
            {
                $"Occupancy records: {dto.Pagination.TotalCount}",
                $"Distinct occupants: {dto.DistinctOccupants}",
                $"Distinct houses: {dto.DistinctHouses}"
            };
            document.Columns = HistoryColumns();
            document.Rows = dto.Rows.Select(HistoryRow).ToList();
            return document;
        }

        public static List<string> HistoryColumns() => new List<string>
        {
            "Lane", "House No", "Occupant", "Occupant Type", "Number",
            "Occupancy Start", "Occupancy End", "Duration (days)", "Status", "Semester"
        };

        public static List<string> HistoryRow(OccupancyHistoryReportRow row) => new List<string>
        {
            row.LaneName,
            row.HouseNumber,
            row.OccupantName,
            row.OccupantType.ToString(),
            row.OccupantNumber ?? string.Empty,
            row.OccupancyStartDate.ToString(DateFormat),
            row.OccupancyEndDate?.ToString(DateFormat) ?? "Current",
            row.DurationDays.ToString(),
            row.IsCurrent ? "Current" : row.Status,
            row.SemesterName ?? string.Empty
        };

        public static List<string> HouseOccupancySummaryLines(OccupancySummaryReportRow summary) => new List<string>
        {
            $"Houses: {summary.TotalHouses}",
            $"Occupied: {summary.OccupiedHouses}",
            $"Empty: {summary.EmptyHouses}",
            $"Capacity: {summary.TotalCapacity}",
            $"Occupied spaces: {summary.OccupiedSpaces}",
            $"Available spaces: {summary.AvailableSpaces}",
            $"Utilization: {summary.OccupancyPercentage.ToString("0.##")}%"
        };

        public static ReportTableDocument Base(AccommodationReportBaseDto dto) => new ReportTableDocument
        {
            ReportTitle = dto.ReportTitle,
            GeneratedBy = dto.GeneratedBy,
            GeneratedAtUtc = dto.GeneratedAtUtc,
            Filters = dto.AppliedFilters
                .Select(f => new ReportFilterEntry { Label = f.Label, Value = f.Value })
                .ToList()
        };

        public static ReportTableDocument FromHouseHistory(HouseOccupancyHistoryReportDto dto)
        {
            var document = Base(dto);
            document.SummaryLines = new List<string>
            {
                $"Lane: {dto.LaneName}",
                $"House: {dto.HouseNumber}{(string.IsNullOrWhiteSpace(dto.HouseName) ? string.Empty : $" ({dto.HouseName})")}",
                $"Status: {dto.HouseStatus}",
                $"Capacity: {dto.Capacity}",
                $"Current occupants: {dto.CurrentOccupants}",
                $"Total occupancy records: {dto.TotalStays}"
            };
            document.Columns = HistoryColumns();
            document.Rows = dto.Rows.Select(HistoryRow).ToList();
            return document;
        }

        public static ReportTableDocument FromByPeriod(OccupancyByPeriodReportDto dto)
        {
            var document = Base(dto);
            document.SummaryLines = new List<string> { $"Period: {dto.PeriodLabel}" };
            document.SummaryLines.AddRange(HouseOccupancySummaryLines(dto.Summary));
            document.Columns = new List<string>
            {
                "Lane", "House No", "House Name", "House Status", "Capacity",
                "Occupied in period", "Available in period", "Occupants in period"
            };

            document.Rows = dto.Rows.Select(r => new List<string>
            {
                r.LaneName,
                r.HouseNumber,
                r.HouseName ?? string.Empty,
                r.Status,
                r.Capacity.ToString(),
                r.OccupiedInPeriod.ToString(),
                r.AvailableInPeriod.ToString(),
                r.OccupantsInPeriod
            }).ToList();

            return document;
        }

        public static ReportTableDocument FromOccupantHistory(OccupantAccommodationHistoryReportDto dto)
        {
            var document = Base(dto);

            if (string.Equals(dto.Mode, "Detail", StringComparison.OrdinalIgnoreCase))
            {
                var occupant = dto.SelectedOccupant;
                document.SummaryLines = new List<string>
                {
                    $"Occupant: {occupant?.OccupantName}",
                    $"Type: {occupant?.OccupantType}",
                    $"Number: {occupant?.OccupantNumber ?? "-"}",
                    $"Current house: {(string.IsNullOrWhiteSpace(dto.CurrentHouse) ? "None" : dto.CurrentHouse)}",
                    $"Total occupancy records: {dto.Stays.Count}"
                };
                document.Columns = HistoryColumns();
                document.Rows = dto.Stays.Select(HistoryRow).ToList();
                return document;
            }

            document.SummaryLines = new List<string> { $"Matching occupants: {dto.Candidates.Count}" };
            document.Columns = new List<string>
            {
                "Occupant", "Occupant Type", "Number", "Current House", "Stays", "Currently Resident"
            };

            document.Rows = dto.Candidates.Select(c => new List<string>
            {
                c.OccupantName,
                c.OccupantType.ToString(),
                c.OccupantNumber ?? string.Empty,
                c.CurrentHouseNumber ?? "None",
                c.TotalStays.ToString(),
                c.IsCurrent ? "Yes" : "No"
            }).ToList();

            return document;
        }

        public static ReportTableDocument FromUtilization(HouseUtilizationSummaryReportDto dto)
        {
            var document = Base(dto);
            var summary = dto.Summary;

            document.Columns = new List<string> { "Metric", "Value" };
            document.Rows = new List<List<string>>
            {
                new List<string> { "Total houses", summary.TotalHouses.ToString() },
                new List<string> { "Occupied houses", summary.OccupiedHouses.ToString() },
                new List<string> { "Empty houses", summary.EmptyHouses.ToString() },
                new List<string> { "Total capacity (spaces)", summary.TotalCapacity.ToString() },
                new List<string> { "Occupied spaces", summary.OccupiedSpaces.ToString() },
                new List<string> { "Available spaces", summary.AvailableSpaces.ToString() },
                new List<string> { "Houses at full capacity", summary.HousesAtFullCapacity.ToString() },
                new List<string> { "Houses with available capacity", summary.HousesWithAvailableCapacity.ToString() },
                new List<string> { "Houses never occupied", summary.HousesNeverOccupied.ToString() },
                new List<string> { "Occupancy percentage", $"{summary.OccupancyPercentage.ToString("0.##")}%" }
            };

            document.SummaryLines = HouseOccupancySummaryLines(summary);
            return document;
        }
    }
}
