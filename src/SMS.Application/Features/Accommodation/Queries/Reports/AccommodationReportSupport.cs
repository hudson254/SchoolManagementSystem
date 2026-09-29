using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Reporting;
using SMS.Domain.Rules;
using System;
using System.Collections.Generic;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// Shared filter contract for the accommodation report queries. All filters
    /// are optional; handlers validate the ones that matter for their report.
    /// </summary>
    public abstract class AccommodationReportQueryBase
    {
        public Guid? LaneId { get; set; }
        public Guid? HouseId { get; set; }
        public string? Status { get; set; }
        public OccupantType? OccupantType { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid? AcademicYearId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Maps the query to repository filters, clamping the page and page size.
        /// <paramref name="maxPageSize"/> is used by exports so a single file can
        /// never grow unbounded.
        /// </summary>
        public AccommodationReportFilters ToFilters(int? maxPageSize = null)
        {
            var size = PageSize < 1 ? 50 : PageSize;
            if (maxPageSize.HasValue)
                size = Math.Min(size, maxPageSize.Value);

            return new AccommodationReportFilters
            {
                LaneId = LaneId,
                HouseId = HouseId,
                Status = Status,
                OccupantType = OccupantType,
                SemesterId = SemesterId,
                AcademicYearId = AcademicYearId,
                FromDate = FromDate,
                ToDate = ToDate,
                SearchTerm = SearchTerm,
                Page = Page < 1 ? 1 : Page,
                PageSize = size
            };
        }

        /// <summary>Validates From/To ordering; throws a user-friendly 400 on failure.</summary>
        public void ValidatePeriod()
        {
            var error = OccupancyDateRules.ValidatePeriod(FromDate, ToDate);
            if (error != null)
                throw new ValidationException(error);
        }
    }

    /// <summary>Shared metadata/filename helpers for the report handlers.</summary>
    public static class AccommodationReportSupport
    {
        /// <summary>
        /// Builds the filter list echoed in previews and export headers.
        /// <paramref name="resolvedLabels"/> maps filter keys (LaneId, HouseId,
        /// SemesterId, AcademicYearId) to human-readable names when the caller
        /// resolved them; otherwise the raw identifier is used.
        /// </summary>
        public static List<ReportFilterAppliedDto> BuildAppliedFilters(
            AccommodationReportFilters filters,
            IReadOnlyDictionary<string, string>? resolvedLabels = null)
        {
            string? Resolve(string key, Guid? value)
            {
                if (!value.HasValue) return null;
                return resolvedLabels != null && resolvedLabels.TryGetValue(key, out var name)
                    ? name
                    : value.Value.ToString();
            }

            var list = new List<ReportFilterAppliedDto>();
            void Add(string label, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    list.Add(new ReportFilterAppliedDto { Label = label, Value = value! });
            }

            Add("Lane", Resolve("LaneId", filters.LaneId));
            Add("House", Resolve("HouseId", filters.HouseId));
            Add("House status", filters.Status);
            Add("Occupant type", filters.OccupantType?.ToString());
            Add("Semester", Resolve("SemesterId", filters.SemesterId));
            Add("Academic year", Resolve("AcademicYearId", filters.AcademicYearId));
            Add("From", filters.FromDate?.ToString("yyyy-MM-dd"));
            Add("To", filters.ToDate?.ToString("yyyy-MM-dd"));
            Add("Search", filters.SearchTerm);

            if (list.Count == 0)
                list.Add(new ReportFilterAppliedDto { Label = "Filters", Value = "None" });

            return list;
        }

        public static ReportPaginationDto BuildPagination(AccommodationReportFilters filters, int totalCount)
        {
            var pageSize = filters.PageSize < 1 ? 50 : filters.PageSize;
            return new ReportPaginationDto
            {
                TotalCount = totalCount,
                Page = filters.Page,
                PageSize = pageSize,
                TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0
            };
        }

        public static void ApplyMeta(
            AccommodationReportBaseDto dto,
            string reportKey,
            string reportTitle,
            AccommodationReportFilters filters,
            string generatedBy,
            DateTime generatedAtUtc,
            IReadOnlyDictionary<string, string>? resolvedLabels = null)
        {
            dto.ReportKey = reportKey;
            dto.ReportTitle = reportTitle;
            dto.GeneratedBy = generatedBy;
            dto.GeneratedAtUtc = generatedAtUtc;
            dto.AppliedFilters = BuildAppliedFilters(filters, resolvedLabels);
        }

        /// <summary>
        /// Builds a meaningful export filename, e.g.
        /// Accommodation_Current_Occupancy_2026-09-28.pdf or
        /// Accommodation_Occupancy_History_2026-01-01_to_2026-09-28.xlsx.
        /// </summary>
        public static string BuildFileName(string fileStem, DateTime? from, DateTime? to, string extension)
        {
            string stamp;
            if (from.HasValue || to.HasValue)
            {
                var fromPart = from?.ToString("yyyy-MM-dd") ?? "Start";
                var toPart = to?.ToString("yyyy-MM-dd") ?? "Today";
                stamp = $"_{fromPart}_to_{toPart}";
            }
            else
            {
                stamp = $"_{DateTime.UtcNow:yyyy-MM-dd}";
            }

            return $"{fileStem}{stamp}.{extension}";
        }

        /// <summary>
        /// Best-effort resolution of filter identifiers to display names for the
        /// report header / export files.
        /// </summary>
        public static async System.Threading.Tasks.Task<Dictionary<string, string>> ResolveFilterLabelsAsync(
            Domain.Interfaces.IAccommodationRepository accommodationRepository,
            Domain.Interfaces.ISemesterRepository semesterRepository,
            AccommodationReportFilters filters,
            System.Threading.CancellationToken cancellationToken)
        {
            var labels = new Dictionary<string, string>();

            if (filters.LaneId.HasValue)
            {
                var lane = await accommodationRepository.GetLaneByIdAsync(filters.LaneId.Value, cancellationToken);
                if (lane != null) labels["LaneId"] = lane.LaneName;
            }

            if (filters.HouseId.HasValue)
            {
                var house = await accommodationRepository.GetHouseByIdAsync(filters.HouseId.Value, cancellationToken);
                if (house != null)
                {
                    labels["HouseId"] = house.HouseName != null
                        ? $"House {house.HouseNumber} ({house.HouseName})"
                        : $"House {house.HouseNumber}";
                }
            }

            if (filters.SemesterId.HasValue)
            {
                var semester = await semesterRepository.GetByIdAsync(filters.SemesterId.Value, cancellationToken);
                if (semester != null) labels["SemesterId"] = semester.Name;
            }

            return labels;
        }

        /// <summary>Resolves the display name of the user generating a report.</summary>
        public static string ResolveGeneratedBy(Domain.Interfaces.ICurrentUserService currentUser)
        {
            if (currentUser == null) return "unknown";
            if (!string.IsNullOrWhiteSpace(currentUser.Username)) return currentUser.Username;
            if (!string.IsNullOrWhiteSpace(currentUser.Email)) return currentUser.Email;
            if (!string.IsNullOrWhiteSpace(currentUser.UserId)) return currentUser.UserId;
            return "unknown";
        }

        /// <summary>
        /// Records report generation in the audit trail: user, report type and
        /// the filters used. Report data itself is never written to audit logs.
        /// </summary>
        public static async System.Threading.Tasks.Task AuditAsync(
            Domain.Interfaces.IAuditService auditService,
            string reportKey,
            AccommodationReportFilters filters,
            string generatedBy)
        {
            var parts = new List<string> { $"report={reportKey}", $"user={generatedBy}" };
            if (filters.LaneId.HasValue) parts.Add($"laneId={filters.LaneId}");
            if (filters.HouseId.HasValue) parts.Add($"houseId={filters.HouseId}");
            if (!string.IsNullOrWhiteSpace(filters.Status)) parts.Add($"status={filters.Status}");
            if (filters.OccupantType.HasValue) parts.Add($"occupantType={filters.OccupantType}");
            if (filters.SemesterId.HasValue) parts.Add($"semesterId={filters.SemesterId}");
            if (filters.AcademicYearId.HasValue) parts.Add($"academicYearId={filters.AcademicYearId}");
            if (filters.FromDate.HasValue) parts.Add($"from={filters.FromDate:yyyy-MM-dd}");
            if (filters.ToDate.HasValue) parts.Add($"to={filters.ToDate:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm)) parts.Add($"search={filters.SearchTerm}");
            parts.Add($"page={filters.Page}");
            parts.Add($"pageSize={filters.PageSize}");

            await auditService.LogAsync("GenerateReport", "AccommodationReport", string.Join("; ", parts));
        }
    }
}
