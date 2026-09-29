using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// F. Occupancy by Period report — who occupied which houses during a
    /// selected period (semester, academic year, custom date range). When no
    /// period is supplied the report covers all recorded occupancy.
    /// </summary>
    public class GetOccupancyByPeriodReportQuery : AccommodationReportQueryBase, IRequest<OccupancyByPeriodReportDto>
    {
    }

    public class GetOccupancyByPeriodReportHandler : IRequestHandler<GetOccupancyByPeriodReportQuery, OccupancyByPeriodReportDto>
    {
        private readonly IAccommodationReportRepository _reportRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<GetOccupancyByPeriodReportHandler> _logger;

        public GetOccupancyByPeriodReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<GetOccupancyByPeriodReportHandler> logger)
        {
            _reportRepository = reportRepository;
            _accommodationRepository = accommodationRepository;
            _semesterRepository = semesterRepository;
            _auditService = auditService;
            _currentUser = currentUserService;
            _logger = logger;
        }

        public async Task<OccupancyByPeriodReportDto> Handle(
            GetOccupancyByPeriodReportQuery request,
            CancellationToken cancellationToken)
        {
            request.ValidatePeriod();

            var filters = request.ToFilters();
            var (rows, summary) = await _reportRepository.GetOccupancyByPeriodReportAsync(
                filters, filters.FromDate, filters.ToDate, cancellationToken);

            var labels = await AccommodationReportSupport.ResolveFilterLabelsAsync(
                _accommodationRepository, _semesterRepository, filters, cancellationToken);

            var generatedBy = AccommodationReportSupport.ResolveGeneratedBy(_currentUser);
            var dto = new OccupancyByPeriodReportDto
            {
                PeriodStart = filters.FromDate,
                PeriodEnd = filters.ToDate,
                PeriodLabel = BuildPeriodLabel(filters, labels),
                Summary = summary,
                Rows = rows.Items,
                Pagination = AccommodationReportSupport.BuildPagination(filters, rows.TotalCount)
            };

            AccommodationReportSupport.ApplyMeta(
                dto, "occupancy-by-period", "Occupancy by Period", filters, generatedBy, DateTime.UtcNow, labels);

            await AccommodationReportSupport.AuditAsync(_auditService, "occupancy-by-period", filters, generatedBy);

            _logger.LogInformation(
                "Occupancy by period report generated: {Period}, {Houses} houses, {Occupied} occupied spaces",
                dto.PeriodLabel, summary.TotalHouses, summary.OccupiedSpaces);

            return dto;
        }

        private static string BuildPeriodLabel(
            Domain.Reporting.AccommodationReportFilters filters,
            IReadOnlyDictionary<string, string> labels)
        {
            if (filters.FromDate.HasValue || filters.ToDate.HasValue)
            {
                var fromPart = filters.FromDate?.ToString("dd MMM yyyy") ?? "Start";
                var toPart = filters.ToDate?.ToString("dd MMM yyyy") ?? DateTime.UtcNow.ToString("dd MMM yyyy");
                return $"{fromPart} to {toPart}";
            }

            if (filters.SemesterId.HasValue && labels.TryGetValue("SemesterId", out var semesterName))
                return semesterName;

            return "All recorded occupancy";
        }
    }
}
