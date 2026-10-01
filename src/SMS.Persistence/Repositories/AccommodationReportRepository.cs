using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Domain.Reporting;
using SMS.Domain.Rules;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    /// <summary>
    /// Database-side read models for the Accommodation reports.
    ///
    /// Every query projects only the columns it needs, filters and paginates in
    /// PostgreSQL, and fetches occupants for a page of houses in a single extra
    /// query (never per-house). Tenant isolation comes from the shared DbContext
    /// query filter and PostgreSQL row-level security; IsDeleted is always
    /// applied explicitly because the global tenant filter replaced the earlier
    /// soft-delete filters for these entity types.
    /// </summary>
    public class AccommodationReportRepository : IAccommodationReportRepository
    {
        private const string ActiveStatus = "Active";
        private const int OccupantSearchCap = 50;

        private readonly ApplicationDbContext _context;

        public AccommodationReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== Shared query building blocks =====

        private IQueryable<House> HouseQuery(AccommodationReportFilters filters)
        {
            var query = _context.Set<House>().Where(h => !h.IsDeleted);

            if (filters.LaneId.HasValue)
                query = query.Where(h => h.LaneId == filters.LaneId.Value);
            if (filters.HouseId.HasValue)
                query = query.Where(h => h.Id == filters.HouseId.Value);
            if (!string.IsNullOrWhiteSpace(filters.Status))
                query = query.Where(h => h.Status == filters.Status);

            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var term = filters.SearchTerm.Trim().ToLower();
                query = query.Where(h =>
                    h.HouseNumber.ToLower().Contains(term) ||
                    (h.HouseName != null && h.HouseName.ToLower().Contains(term)) ||
                    h.Lane.LaneName.ToLower().Contains(term));
            }

            return query;
        }

        private IQueryable<AccommodationAssignment> ActiveAssignmentQuery()
        {
            return _context.Set<AccommodationAssignment>()
                .Where(a => !a.IsDeleted && a.Status == ActiveStatus);
        }

        /// <summary>
        /// Applies the assignment-level (occupancy) filters. House-set filters
        /// (LaneId/HouseId/Status/SearchTerm) are applied through the house query
        /// so that summary and rows always agree on the house set.
        /// </summary>
        private static IQueryable<AccommodationAssignment> ApplyAssignmentFilters(
            IQueryable<AccommodationAssignment> query,
            AccommodationReportFilters filters)
        {
            if (filters.OccupantType.HasValue)
                query = query.Where(a => a.OccupantType == filters.OccupantType.Value);
            if (filters.SemesterId.HasValue)
                query = query.Where(a => a.SemesterId == filters.SemesterId.Value);
            if (filters.AcademicYearId.HasValue)
            {
                var yearId = filters.AcademicYearId.Value;
                query = query.Where(a => a.Semester.AcademicYearId == yearId);
            }

            return query;
        }

        private IQueryable<AccommodationAssignment> OccupancyEvidenceQuery(AccommodationReportFilters filters)
        {
            return ApplyAssignmentFilters(ActiveAssignmentQuery(), filters);
        }

        private static IQueryable<T> Page<T>(IQueryable<T> query, AccommodationReportFilters filters)
        {
            var page = filters.Page < 1 ? 1 : filters.Page;
            var size = filters.PageSize < 1 ? 50 : filters.PageSize;
            return query.Skip((page - 1) * size).Take(size);
        }

        private static string BuildName(string? title, string? first, string? middle, string? last)
        {
            return string.Join(" ", new[] { title, first, middle, last }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim()));
        }

        private static DateTime OccupancyStart(DateTime? moveInDate, DateTime assignmentDate, DateTime assignedDate)
        {
            if (moveInDate.HasValue) return moveInDate.Value;
            return assignmentDate != default ? assignmentDate : assignedDate;
        }

        // ===== Current / Occupied / Empty house reports =====

        public async Task<(ReportPage<HouseOccupancyReportRow> Page, OccupancySummaryReportRow Summary)> GetHouseOccupancyReportAsync(
            AccommodationReportFilters filters,
            HouseOccupancyScope scope,
            CancellationToken cancellationToken = default)
        {
            var houseQuery = HouseQuery(filters);
            var evidence = OccupancyEvidenceQuery(filters);

            IQueryable<House> scoped = scope switch
            {
                HouseOccupancyScope.Occupied =>
                    houseQuery.Where(h => evidence.Select(a => a.HouseId).Contains(h.Id)),
                HouseOccupancyScope.Empty =>
                    houseQuery.Where(h => !evidence.Select(a => a.HouseId).Contains(h.Id)),
                _ => houseQuery
            };

            // ---- Summary: one lightweight aggregation over the filtered house set ----
            var summary = new OccupancySummaryReportRow();
            var stats = await houseQuery
                .Select(h => new
                {
                    h.Capacity,
                    ActiveCount = evidence.Count(a => a.HouseId == h.Id),
                    TotalRecords = _context.Set<AccommodationAssignment>()
                        .Count(a => a.HouseId == h.Id && !a.IsDeleted)
                })
                .ToListAsync(cancellationToken);

            summary.TotalHouses = stats.Count;
            summary.OccupiedHouses = stats.Count(s => s.ActiveCount > 0);
            summary.EmptyHouses = summary.TotalHouses - summary.OccupiedHouses;
            summary.TotalCapacity = stats.Sum(s => s.Capacity);
            summary.OccupiedSpaces = stats.Sum(s => s.ActiveCount);
            summary.AvailableSpaces = Math.Max(0, summary.TotalCapacity - summary.OccupiedSpaces);
            summary.HousesAtFullCapacity = AccommodationCapacityRules.CountAtFullCapacity(
                stats, s => s.Capacity, s => s.ActiveCount);
            summary.HousesWithAvailableCapacity = AccommodationCapacityRules.CountWithAvailableCapacity(
                stats, s => s.Capacity, s => s.ActiveCount);
            summary.HousesNeverOccupied = AccommodationCapacityRules.CountNeverOccupied(
                stats, s => s.TotalRecords);
            summary.OccupancyPercentage = summary.TotalCapacity > 0
                ? Math.Round((decimal)summary.OccupiedSpaces * 100 / summary.TotalCapacity, 2)
                : 0m;

            // ---- Paged rows ----
            var totalCount = await scoped.CountAsync(cancellationToken);

            var pageRows = await Page(scoped
                    .OrderBy(h => h.Lane.LaneName)
                    .ThenBy(h => h.HouseNumberNumeric)
                    .ThenBy(h => h.HouseNumber)
                    .Select(h => new
                    {
                        h.Id,
                        h.HouseNumber,
                        h.HouseName,
                        h.LaneId,
                        LaneName = h.Lane.LaneName,
                        h.Status,
                        h.Capacity,
                        h.IsAvailable,
                        h.IsEnabled,
                        h.OccupiedDate,
                        h.VacatedDate
                    }), filters)
                .ToListAsync(cancellationToken);

            var rows = pageRows.Select(h => new HouseOccupancyReportRow
            {
                HouseId = h.Id,
                HouseNumber = h.HouseNumber,
                HouseName = h.HouseName,
                LaneId = h.LaneId,
                LaneName = h.LaneName,
                Status = h.Status,
                Capacity = h.Capacity,
                IsAvailable = h.IsAvailable,
                IsEnabled = h.IsEnabled,
                OccupiedDate = h.OccupiedDate,
                VacatedDate = h.VacatedDate
            }).ToList();

            if (rows.Count > 0)
            {
                var houseIds = rows.Select(r => r.HouseId).ToList();

                // Current occupants for the whole page in ONE query.
                var occupantProjections = await ApplyAssignmentFilters(ActiveAssignmentQuery(), filters)
                    .Where(a => houseIds.Contains(a.HouseId))
                    .OrderBy(a => a.AssignmentDate)
                    .Select(a => new
                    {
                        a.Id,
                        a.HouseId,
                        a.OccupantType,
                        a.StudentId,
                        a.LecturerId,
                        a.AssignmentDate,
                        a.AssignedDate,
                        a.MoveInDate,
                        a.CheckInDate,
                        a.Status,
                        a.SemesterId,
                        SemesterName = a.Semester.Name,
                        StTitle = a.Student != null ? a.Student.Title : null,
                        StFirst = a.Student != null ? a.Student.FirstName : null,
                        StMiddle = a.Student != null ? a.Student.MiddleName : null,
                        StLast = a.Student != null ? a.Student.LastName : null,
                        StNumber = a.Student != null ? a.Student.StudentNumber : null,
                        LeTitle = a.Lecturer != null ? a.Lecturer.Title : null,
                        LeFirst = a.Lecturer != null ? a.Lecturer.FirstName : null,
                        LeMiddle = a.Lecturer != null ? a.Lecturer.MiddleName : null,
                        LeLast = a.Lecturer != null ? a.Lecturer.LastName : null,
                        LeNumber = a.Lecturer != null ? a.Lecturer.EmployeeNumber : null
                    })
                    .ToListAsync(cancellationToken);

                var occupantsByHouse = occupantProjections
                    .GroupBy(a => a.HouseId)
                    .ToDictionary(g => g.Key, g => g.Select(a => new HouseOccupantReportRow
                    {
                        AssignmentId = a.Id,
                        OccupantId = a.OccupantType == OccupantType.Student ? a.StudentId ?? a.Id : a.LecturerId ?? a.Id,
                        OccupantType = a.OccupantType,
                        OccupantName = a.OccupantType == OccupantType.Student
                            ? BuildName(a.StTitle, a.StFirst, a.StMiddle, a.StLast)
                            : BuildName(a.LeTitle, a.LeFirst, a.LeMiddle, a.LeLast),
                        OccupantNumber = a.OccupantType == OccupantType.Student ? a.StNumber : a.LeNumber,
                        AllocationDate = OccupancyStart(a.MoveInDate, a.AssignmentDate, a.AssignedDate),
                        MoveInDate = a.MoveInDate,
                        CheckInDate = a.CheckInDate,
                        AssignmentStatus = a.Status,
                        SemesterId = a.SemesterId,
                        SemesterName = a.SemesterName
                    }).ToList());

                foreach (var row in rows)
                {
                    if (occupantsByHouse.TryGetValue(row.HouseId, out var occupants))
                        row.CurrentOccupants = occupants;

                    row.OccupiedCount = row.CurrentOccupants.Count;
                    row.AvailableSpaces = Math.Max(0, row.Capacity - row.OccupiedCount);
                    row.IsOccupied = row.OccupiedCount > 0;
                    row.OccupancyStatus = row.OccupiedCount == 0
                        ? "Empty"
                        : row.OccupiedCount >= row.Capacity ? "Full" : "Occupied";
                }

                // Empty Houses report: enrich with the last occupant and the
                // total number of stays the house has ever had (history stats).
                if (scope == HouseOccupancyScope.Empty)
                {
                    var historyProjections = await _context.Set<AccommodationAssignment>()
                        .Where(a => !a.IsDeleted && houseIds.Contains(a.HouseId))
                        .Select(a => new
                        {
                            a.HouseId,
                            a.VacatedDate,
                            a.OccupantType,
                            StTitle = a.Student != null ? a.Student.Title : null,
                            StFirst = a.Student != null ? a.Student.FirstName : null,
                            StMiddle = a.Student != null ? a.Student.MiddleName : null,
                            StLast = a.Student != null ? a.Student.LastName : null,
                            StNumber = a.Student != null ? a.Student.StudentNumber : null,
                            LeTitle = a.Lecturer != null ? a.Lecturer.Title : null,
                            LeFirst = a.Lecturer != null ? a.Lecturer.FirstName : null,
                            LeMiddle = a.Lecturer != null ? a.Lecturer.MiddleName : null,
                            LeLast = a.Lecturer != null ? a.Lecturer.LastName : null,
                            LeNumber = a.Lecturer != null ? a.Lecturer.EmployeeNumber : null
                        })
                        .ToListAsync(cancellationToken);

                    foreach (var row in rows)
                    {
                        var houseHistory = historyProjections.Where(x => x.HouseId == row.HouseId).ToList();
                        row.HistoricalOccupantCount = houseHistory.Count;

                        var last = houseHistory
                            .Where(x => x.VacatedDate.HasValue)
                            .OrderByDescending(x => x.VacatedDate)
                            .FirstOrDefault();

                        if (last != null)
                        {
                            row.LastOccupancyEndDate = last.VacatedDate;
                            row.LastOccupantType = last.OccupantType;
                            row.LastOccupantName = last.OccupantType == OccupantType.Student
                                ? BuildName(last.StTitle, last.StFirst, last.StMiddle, last.StLast)
                                : BuildName(last.LeTitle, last.LeFirst, last.LeMiddle, last.LeLast);
                            row.LastOccupantNumber = last.OccupantType == OccupantType.Student
                                ? last.StNumber
                                : last.LeNumber;
                        }
                    }
                }
            }

            var page = new ReportPage<HouseOccupancyReportRow>
            {
                Items = rows,
                TotalCount = totalCount,
                Page = filters.Page < 1 ? 1 : filters.Page,
                PageSize = filters.PageSize < 1 ? 50 : filters.PageSize
            };

            return (page, summary);
        }

        // ===== Occupancy history (date-range overlap) =====

        /// <summary>
        /// Lightweight projection shape used to translate occupancy rows in
        /// SQL while keeping display-name composition in memory (avoids fragile
        /// string concatenation translation differences across providers).
        /// </summary>
        private sealed class AssignmentHistoryProjection
        {
            public Guid Id { get; set; }
            public Guid HouseId { get; set; }
            public string HouseNumber { get; set; } = string.Empty;
            public string? HouseName { get; set; }
            public string LaneName { get; set; } = string.Empty;
            public OccupantType OccupantType { get; set; }
            public Guid? StudentId { get; set; }
            public Guid? LecturerId { get; set; }
            public DateTime? MoveInDate { get; set; }
            public DateTime AssignmentDate { get; set; }
            public DateTime AssignedDate { get; set; }
            public DateTime? VacatedDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public Guid SemesterId { get; set; }
            public string SemesterName { get; set; } = string.Empty;
            public string? AcademicYearName { get; set; }
            public string? StTitle { get; set; }
            public string? StFirst { get; set; }
            public string? StMiddle { get; set; }
            public string? StLast { get; set; }
            public string? StNumber { get; set; }
            public string? LeTitle { get; set; }
            public string? LeFirst { get; set; }
            public string? LeMiddle { get; set; }
            public string? LeLast { get; set; }
            public string? LeNumber { get; set; }
        }

        private IQueryable<AssignmentHistoryProjection> HistoryProjectionQuery(IQueryable<AccommodationAssignment> source)
        {
            return source.Select(a => new AssignmentHistoryProjection
            {
                Id = a.Id,
                HouseId = a.HouseId,
                HouseNumber = a.House.HouseNumber,
                HouseName = a.House.HouseName,
                LaneName = a.House.Lane.LaneName,
                OccupantType = a.OccupantType,
                StudentId = a.StudentId,
                LecturerId = a.LecturerId,
                MoveInDate = a.MoveInDate,
                AssignmentDate = a.AssignmentDate,
                AssignedDate = a.AssignedDate,
                VacatedDate = a.VacatedDate,
                Status = a.Status,
                SemesterId = a.SemesterId,
                SemesterName = a.Semester.Name,
                AcademicYearName = a.Semester.AcademicYear != null ? a.Semester.AcademicYear.Name : null,
                StTitle = a.Student != null ? a.Student.Title : null,
                StFirst = a.Student != null ? a.Student.FirstName : null,
                StMiddle = a.Student != null ? a.Student.MiddleName : null,
                StLast = a.Student != null ? a.Student.LastName : null,
                StNumber = a.Student != null ? a.Student.StudentNumber : null,
                LeTitle = a.Lecturer != null ? a.Lecturer.Title : null,
                LeFirst = a.Lecturer != null ? a.Lecturer.FirstName : null,
                LeMiddle = a.Lecturer != null ? a.Lecturer.MiddleName : null,
                LeLast = a.Lecturer != null ? a.Lecturer.LastName : null,
                LeNumber = a.Lecturer != null ? a.Lecturer.EmployeeNumber : null
            });
        }

        private static List<OccupancyHistoryReportRow> ToHistoryRows(IEnumerable<AssignmentHistoryProjection> items)
        {
            var now = DateTime.UtcNow;
            var rows = new List<OccupancyHistoryReportRow>();
            foreach (var p in items)
            {
                var start = OccupancyStart(p.MoveInDate, p.AssignmentDate, p.AssignedDate);
                rows.Add(new OccupancyHistoryReportRow
                {
                    AssignmentId = p.Id,
                    HouseId = p.HouseId,
                    HouseNumber = p.HouseNumber,
                    HouseName = p.HouseName,
                    LaneName = p.LaneName,
                    OccupantId = p.OccupantType == OccupantType.Student ? p.StudentId ?? p.Id : p.LecturerId ?? p.Id,
                    OccupantType = p.OccupantType,
                    OccupantName = p.OccupantType == OccupantType.Student
                        ? BuildName(p.StTitle, p.StFirst, p.StMiddle, p.StLast)
                        : BuildName(p.LeTitle, p.LeFirst, p.LeMiddle, p.LeLast),
                    OccupantNumber = p.OccupantType == OccupantType.Student ? p.StNumber : p.LeNumber,
                    OccupancyStartDate = start,
                    OccupancyEndDate = p.VacatedDate,
                    IsCurrent = p.Status == ActiveStatus,
                    DurationDays = OccupancyDateRules.DurationInDays(start, p.VacatedDate, now),
                    Status = p.Status,
                    SemesterId = p.SemesterId,
                    SemesterName = p.SemesterName,
                    AcademicYearName = p.AcademicYearName
                });
            }
            return rows;
        }

        private static IQueryable<AccommodationAssignment> ApplySearchTerm(
            IQueryable<AccommodationAssignment> query,
            string searchTerm)
        {
            var term = searchTerm.Trim().ToLower();
            return query.Where(a =>
                (a.Student != null && (a.Student.FirstName.ToLower().Contains(term)
                    || a.Student.LastName.ToLower().Contains(term)
                    || a.Student.StudentNumber.ToLower().Contains(term)
                    || (a.Student.MiddleName != null && a.Student.MiddleName.ToLower().Contains(term))))
                || (a.Lecturer != null && (a.Lecturer.FirstName.ToLower().Contains(term)
                    || a.Lecturer.LastName.ToLower().Contains(term)
                    || a.Lecturer.EmployeeNumber.ToLower().Contains(term)))
                || a.House.HouseNumber.ToLower().Contains(term)
                || (a.House.HouseName != null && a.House.HouseName.ToLower().Contains(term))
                || a.House.Lane.LaneName.ToLower().Contains(term));
        }

        public async Task<(ReportPage<OccupancyHistoryReportRow> Page, int DistinctOccupants, int DistinctHouses)> GetOccupancyHistoryReportAsync(
            AccommodationReportFilters filters,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken = default)
        {
            // Proper date-range overlap: start <= periodEnd AND (end IS NULL OR end >= periodStart).
            var query = _context.Set<AccommodationAssignment>()
                .Where(a => !a.IsDeleted
                    && (a.MoveInDate ?? a.AssignmentDate) <= periodEnd
                    && (a.VacatedDate == null || a.VacatedDate >= periodStart));

            query = ApplyAssignmentFilters(query, filters);

            if (filters.HouseId.HasValue)
                query = query.Where(a => a.HouseId == filters.HouseId.Value);
            if (filters.LaneId.HasValue)
                query = query.Where(a => a.LaneId == filters.LaneId.Value);
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                query = ApplySearchTerm(query, filters.SearchTerm);

            var totalCount = await query.CountAsync(cancellationToken);
            var distinctOccupants = await query
                .Select(a => a.StudentId ?? a.LecturerId)
                .Distinct()
                .CountAsync(cancellationToken);
            var distinctHouses = await query
                .Select(a => a.HouseId)
                .Distinct()
                .CountAsync(cancellationToken);

            var ordered = query
                .OrderBy(a => a.MoveInDate ?? a.AssignmentDate)
                .ThenBy(a => a.House.HouseNumber);

            var projections = await Page(HistoryProjectionQuery(ordered), filters)
                .ToListAsync(cancellationToken);

            var page = new ReportPage<OccupancyHistoryReportRow>
            {
                Items = ToHistoryRows(projections),
                TotalCount = totalCount,
                Page = filters.Page < 1 ? 1 : filters.Page,
                PageSize = filters.PageSize < 1 ? 50 : filters.PageSize
            };

            return (page, distinctOccupants, distinctHouses);
        }

        // ===== House occupancy history (one house, all stays) =====

        public async Task<(ReportPage<OccupancyHistoryReportRow> Page, int CurrentOccupants)> GetHouseHistoryReportAsync(
            Guid houseId,
            AccommodationReportFilters filters,
            CancellationToken cancellationToken = default)
        {
            // Current occupants are counted from the authoritative occupancy records
            // (active assignments) rather than from the current page of history.
            var currentOccupants = await ActiveAssignmentQuery()
                .CountAsync(a => a.HouseId == houseId, cancellationToken);

            var query = _context.Set<AccommodationAssignment>()
                .Where(a => !a.IsDeleted && a.HouseId == houseId);

            query = ApplyAssignmentFilters(query, filters);

            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                query = ApplySearchTerm(query, filters.SearchTerm);

            var totalCount = await query.CountAsync(cancellationToken);

            var ordered = query
                .OrderBy(a => a.MoveInDate ?? a.AssignmentDate)
                .ThenBy(a => a.AssignmentDate);

            var projections = await Page(HistoryProjectionQuery(ordered), filters)
                .ToListAsync(cancellationToken);

            return (new ReportPage<OccupancyHistoryReportRow>
            {
                Items = ToHistoryRows(projections),
                TotalCount = totalCount,
                Page = filters.Page < 1 ? 1 : filters.Page,
                PageSize = filters.PageSize < 1 ? 50 : filters.PageSize
            }, currentOccupants);
        }

        // ===== Occupancy by period =====

        public async Task<(ReportPage<OccupancyByPeriodHouseRow> Rows, OccupancySummaryReportRow Summary)> GetOccupancyByPeriodReportAsync(
            AccommodationReportFilters filters,
            DateTime? periodStart,
            DateTime? periodEnd,
            CancellationToken cancellationToken = default)
        {
            var (start, end) = OccupancyDateRules.NormalizePeriod(periodStart, periodEnd);

            var houseQuery = HouseQuery(filters);
            var houseIds = houseQuery.Select(h => h.Id);

            var overlapQuery = ApplyAssignmentFilters(
                    _context.Set<AccommodationAssignment>().Where(a => !a.IsDeleted), filters)
                .Where(a => (a.MoveInDate ?? a.AssignmentDate) <= end
                    && (a.VacatedDate == null || a.VacatedDate >= start))
                .Where(a => houseIds.Contains(a.HouseId));

            // ---- Aggregates (database-side scalar queries, no house rows loaded) ----
            var totalHouses = await houseQuery.CountAsync(cancellationToken);
            var totalCapacity = await houseQuery.Select(h => (int?)h.Capacity).SumAsync(cancellationToken) ?? 0;
            var occupiedHouses = await overlapQuery.Select(a => a.HouseId).Distinct().CountAsync(cancellationToken);
            var occupiedSpaces = await overlapQuery.CountAsync(cancellationToken);

            // Capacity-pressure tiles. These need a per-house figure, so they are the
            // one part of the summary that cannot be a plain scalar: each house is
            // projected with how many occupants it held *inside the selected period*
            // (the same overlap rule the breakdown rows use, so the summary can never
            // disagree with the rows it summarises) and how many assignment records it
            // has ever had. One lightweight row per house, same shape the house and
            // utilization reports already use.
            var capacityStats = await houseQuery
                .Select(h => new
                {
                    h.Capacity,
                    OccupiedInPeriod = overlapQuery.Count(a => a.HouseId == h.Id),
                    TotalRecords = _context.Set<AccommodationAssignment>()
                        .Count(a => a.HouseId == h.Id && !a.IsDeleted)
                })
                .ToListAsync(cancellationToken);

            var summary = new OccupancySummaryReportRow
            {
                TotalHouses = totalHouses,
                OccupiedHouses = occupiedHouses,
                EmptyHouses = Math.Max(0, totalHouses - occupiedHouses),
                TotalCapacity = totalCapacity,
                OccupiedSpaces = occupiedSpaces,
                AvailableSpaces = Math.Max(0, totalCapacity - occupiedSpaces),
                // DEFECT-02: these three were never assigned, so the page rendered
                // them as 0. They now go through the shared rules so they cannot
                // drift from the house reports again.
                HousesAtFullCapacity = AccommodationCapacityRules.CountAtFullCapacity(
                    capacityStats, s => s.Capacity, s => s.OccupiedInPeriod),
                HousesWithAvailableCapacity = AccommodationCapacityRules.CountWithAvailableCapacity(
                    capacityStats, s => s.Capacity, s => s.OccupiedInPeriod),
                HousesNeverOccupied = AccommodationCapacityRules.CountNeverOccupied(
                    capacityStats, s => s.TotalRecords),
                OccupancyPercentage = totalCapacity > 0
                    ? Math.Round((decimal)occupiedSpaces * 100 / totalCapacity, 2)
                    : 0m
            };

            // ---- Per-house breakdown, paged in the database ----
            var paged = await Page(houseQuery
                    .OrderBy(h => h.Lane.LaneName)
                    .ThenBy(h => h.HouseNumberNumeric)
                    .ThenBy(h => h.HouseNumber)
                    .Select(h => new
                    {
                        h.Id,
                        h.HouseNumber,
                        h.HouseName,
                        LaneName = h.Lane.LaneName,
                        h.Status,
                        h.Capacity,
                        OccupiedInPeriod = _context.Set<AccommodationAssignment>().Count(a =>
                            !a.IsDeleted && a.HouseId == h.Id
                            && (a.MoveInDate ?? a.AssignmentDate) <= end
                            && (a.VacatedDate == null || a.VacatedDate >= start)
                            && (!filters.OccupantType.HasValue || a.OccupantType == filters.OccupantType.Value)
                            && (!filters.SemesterId.HasValue || a.SemesterId == filters.SemesterId.Value)
                            && (!filters.AcademicYearId.HasValue || a.Semester.AcademicYearId == filters.AcademicYearId.Value))
                    }), filters)
                .ToListAsync(cancellationToken);

            var rows = paged.Select(h => new OccupancyByPeriodHouseRow
            {
                HouseId = h.Id,
                HouseNumber = h.HouseNumber,
                HouseName = h.HouseName,
                LaneName = h.LaneName,
                Status = h.Status,
                Capacity = h.Capacity,
                OccupiedInPeriod = h.OccupiedInPeriod,
                AvailableInPeriod = Math.Max(0, h.Capacity - h.OccupiedInPeriod)
            }).ToList();

            // Occupant names for the page of houses in ONE query.
            if (rows.Count > 0)
            {
                var pageIds = rows.Select(r => r.HouseId).ToList();
                var occupants = await overlapQuery
                    .Where(a => pageIds.Contains(a.HouseId))
                    .Select(a => new
                    {
                        a.HouseId,
                        a.OccupantType,
                        StTitle = a.Student != null ? a.Student.Title : null,
                        StFirst = a.Student != null ? a.Student.FirstName : null,
                        StMiddle = a.Student != null ? a.Student.MiddleName : null,
                        StLast = a.Student != null ? a.Student.LastName : null,
                        StNumber = a.Student != null ? a.Student.StudentNumber : null,
                        LeTitle = a.Lecturer != null ? a.Lecturer.Title : null,
                        LeFirst = a.Lecturer != null ? a.Lecturer.FirstName : null,
                        LeMiddle = a.Lecturer != null ? a.Lecturer.MiddleName : null,
                        LeLast = a.Lecturer != null ? a.Lecturer.LastName : null,
                        LeNumber = a.Lecturer != null ? a.Lecturer.EmployeeNumber : null
                    })
                    .ToListAsync(cancellationToken);

                foreach (var row in rows)
                {
                    var names = occupants
                        .Where(o => o.HouseId == row.HouseId)
                        .Select(o => o.OccupantType == OccupantType.Student
                            ? $"{BuildName(o.StTitle, o.StFirst, o.StMiddle, o.StLast)} ({o.StNumber})"
                            : $"{BuildName(o.LeTitle, o.LeFirst, o.LeMiddle, o.LeLast)} ({o.LeNumber})");
                    row.OccupantsInPeriod = string.Join(", ", names);
                }
            }

            var pageResult = new ReportPage<OccupancyByPeriodHouseRow>
            {
                Items = rows,
                TotalCount = totalHouses,
                Page = filters.Page < 1 ? 1 : filters.Page,
                PageSize = filters.PageSize < 1 ? 50 : filters.PageSize
            };

            return (pageResult, summary);
        }

        // ===== House utilization summary =====

        public async Task<OccupancySummaryReportRow> GetUtilizationSummaryAsync(
            AccommodationReportFilters filters,
            CancellationToken cancellationToken = default)
        {
            var houseQuery = HouseQuery(filters);
            var evidence = OccupancyEvidenceQuery(filters);

            var stats = await houseQuery
                .Select(h => new
                {
                    h.Capacity,
                    ActiveCount = evidence.Count(a => a.HouseId == h.Id),
                    TotalRecords = _context.Set<AccommodationAssignment>()
                        .Count(a => a.HouseId == h.Id && !a.IsDeleted)
                })
                .ToListAsync(cancellationToken);

            var summary = new OccupancySummaryReportRow
            {
                TotalHouses = stats.Count,
                OccupiedHouses = stats.Count(s => s.ActiveCount > 0),
                TotalCapacity = stats.Sum(s => s.Capacity),
                OccupiedSpaces = stats.Sum(s => s.ActiveCount),
                HousesAtFullCapacity = AccommodationCapacityRules.CountAtFullCapacity(
                    stats, s => s.Capacity, s => s.ActiveCount),
                HousesWithAvailableCapacity = AccommodationCapacityRules.CountWithAvailableCapacity(
                    stats, s => s.Capacity, s => s.ActiveCount),
                HousesNeverOccupied = AccommodationCapacityRules.CountNeverOccupied(
                    stats, s => s.TotalRecords)
            };
            summary.EmptyHouses = summary.TotalHouses - summary.OccupiedHouses;
            summary.AvailableSpaces = Math.Max(0, summary.TotalCapacity - summary.OccupiedSpaces);
            summary.OccupancyPercentage = summary.TotalCapacity > 0
                ? Math.Round((decimal)summary.OccupiedSpaces * 100 / summary.TotalCapacity, 2)
                : 0m;

            return summary;
        }

        // ===== Occupant accommodation history =====

        public async Task<List<OccupantCandidateRow>> SearchOccupantsAsync(
            AccommodationReportFilters filters,
            CancellationToken cancellationToken = default)
        {
            var result = new List<OccupantCandidateRow>();
            var term = filters.SearchTerm?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term))
                return result;

            var limit = filters.PageSize < 1 ? 25 : Math.Min(filters.PageSize, OccupantSearchCap);
            var type = filters.OccupantType;
            var candidates = new List<OccupantCandidateRow>();

            if (type != OccupantType.Lecturer)
            {
                var students = await _context.Set<Student>()
                    .Where(s => !s.IsDeleted
                        && (s.FirstName.ToLower().Contains(term)
                            || s.LastName.ToLower().Contains(term)
                            || s.StudentNumber.ToLower().Contains(term)
                            || (s.MiddleName != null && s.MiddleName.ToLower().Contains(term))))
                    .OrderBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .Take(limit)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.FirstName,
                        s.MiddleName,
                        s.LastName,
                        s.StudentNumber
                    })
                    .ToListAsync(cancellationToken);

                candidates.AddRange(students.Select(s => new OccupantCandidateRow
                {
                    OccupantId = s.Id,
                    OccupantType = OccupantType.Student,
                    OccupantName = BuildName(s.Title, s.FirstName, s.MiddleName, s.LastName),
                    OccupantNumber = s.StudentNumber
                }));
            }

            if (type != OccupantType.Student && candidates.Count < limit)
            {
                var lecturers = await _context.Set<Lecturer>()
                    .Where(l => !l.IsDeleted
                        && (l.FirstName.ToLower().Contains(term)
                            || l.LastName.ToLower().Contains(term)
                            || l.EmployeeNumber.ToLower().Contains(term)
                            || (l.MiddleName != null && l.MiddleName.ToLower().Contains(term))))
                    .OrderBy(l => l.FirstName)
                    .ThenBy(l => l.LastName)
                    .Take(limit)
                    .Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.FirstName,
                        l.MiddleName,
                        l.LastName,
                        l.EmployeeNumber
                    })
                    .ToListAsync(cancellationToken);

                candidates.AddRange(lecturers.Select(l => new OccupantCandidateRow
                {
                    OccupantId = l.Id,
                    OccupantType = OccupantType.Lecturer,
                    OccupantName = BuildName(l.Title, l.FirstName, l.MiddleName, l.LastName),
                    OccupantNumber = l.EmployeeNumber
                }));
            }

            if (candidates.Count == 0)
                return result;

            // Enrich every candidate with stay totals and current house in ONE query.
            var studentIds = candidates.Where(c => c.OccupantType == OccupantType.Student)
                .Select(c => c.OccupantId).ToArray();
            var lecturerIds = candidates.Where(c => c.OccupantType == OccupantType.Lecturer)
                .Select(c => c.OccupantId).ToArray();

            var stays = await _context.Set<AccommodationAssignment>()
                .Where(a => !a.IsDeleted
                    && ((a.StudentId != null && studentIds.Contains(a.StudentId.Value))
                        || (a.LecturerId != null && lecturerIds.Contains(a.LecturerId.Value))))
                .Select(a => new
                {
                    a.StudentId,
                    a.LecturerId,
                    a.Status,
                    a.HouseId,
                    HouseNumber = a.House.HouseNumber
                })
                .ToListAsync(cancellationToken);

            foreach (var candidate in candidates)
            {
                var occupantStays = stays.Where(s =>
                    (candidate.OccupantType == OccupantType.Student && s.StudentId == candidate.OccupantId)
                    || (candidate.OccupantType == OccupantType.Lecturer && s.LecturerId == candidate.OccupantId)).ToList();

                candidate.TotalStays = occupantStays.Count;
                var active = occupantStays.FirstOrDefault(s => s.Status == ActiveStatus);
                if (active != null)
                {
                    candidate.IsCurrent = true;
                    candidate.CurrentHouseId = active.HouseId;
                    candidate.CurrentHouseNumber = active.HouseNumber;
                }
                result.Add(candidate);
            }

            return result
                .OrderBy(c => c.OccupantName)
                .ToList();
        }

        public async Task<OccupantCandidateRow?> GetOccupantAsync(
            Guid occupantId,
            OccupantType occupantType,
            CancellationToken cancellationToken = default)
        {
            OccupantCandidateRow? candidate;

            if (occupantType == OccupantType.Student)
            {
                var student = await _context.Set<Student>()
                    .Where(s => s.Id == occupantId && !s.IsDeleted)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.FirstName,
                        s.MiddleName,
                        s.LastName,
                        s.StudentNumber
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (student == null) return null;

                candidate = new OccupantCandidateRow
                {
                    OccupantId = student.Id,
                    OccupantType = OccupantType.Student,
                    OccupantName = BuildName(student.Title, student.FirstName, student.MiddleName, student.LastName),
                    OccupantNumber = student.StudentNumber
                };
            }
            else
            {
                var lecturer = await _context.Set<Lecturer>()
                    .Where(l => l.Id == occupantId && !l.IsDeleted)
                    .Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.FirstName,
                        l.MiddleName,
                        l.LastName,
                        l.EmployeeNumber
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (lecturer == null) return null;

                candidate = new OccupantCandidateRow
                {
                    OccupantId = lecturer.Id,
                    OccupantType = OccupantType.Lecturer,
                    OccupantName = BuildName(lecturer.Title, lecturer.FirstName, lecturer.MiddleName, lecturer.LastName),
                    OccupantNumber = lecturer.EmployeeNumber
                };
            }

            // Stay totals + current house for this occupant (two cheap queries).
            var occupancyQuery = _context.Set<AccommodationAssignment>()
                .Where(a => !a.IsDeleted
                    && (occupantType == OccupantType.Student
                        ? a.StudentId == occupantId
                        : a.LecturerId == occupantId));

            candidate.TotalStays = await occupancyQuery.CountAsync(cancellationToken);

            var active = await ActiveAssignmentQuery()
                .Where(a => occupantType == OccupantType.Student
                    ? a.StudentId == occupantId
                    : a.LecturerId == occupantId)
                .Select(a => new { a.HouseId, HouseNumber = a.House.HouseNumber })
                .FirstOrDefaultAsync(cancellationToken);

            if (active != null)
            {
                candidate.IsCurrent = true;
                candidate.CurrentHouseId = active.HouseId;
                candidate.CurrentHouseNumber = active.HouseNumber;
            }

            return candidate;
        }

        public async Task<List<OccupancyHistoryReportRow>> GetOccupantStaysAsync(
            Guid occupantId,
            OccupantType occupantType,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<AccommodationAssignment>()
                .Where(a => !a.IsDeleted
                    && (occupantType == OccupantType.Student
                        ? a.StudentId == occupantId
                        : a.LecturerId == occupantId));

            var ordered = query
                .OrderBy(a => a.MoveInDate ?? a.AssignmentDate)
                .ThenBy(a => a.AssignmentDate);

            var projections = await HistoryProjectionQuery(ordered)
                .ToListAsync(cancellationToken);

            return ToHistoryRows(projections);
        }
    }
}
