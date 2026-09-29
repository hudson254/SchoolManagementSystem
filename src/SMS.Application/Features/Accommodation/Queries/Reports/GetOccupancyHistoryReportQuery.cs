using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Domain.Rules;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// D. Occupancy History report — every occupancy record overlapping the
    /// selected period, using proper date-range overlap logic (never simple
    /// date equality). Occupants who entered before the period and left during
    /// it, or entered during it and stayed past it, are both included.
    /// </summary>
    public class GetOccupancyHistoryReportQuery : AccommodationReportQueryBase, IRequest<OccupancyHistoryReportDto>
    {
    }

    public class GetOccupancyHistoryReportHandler : IRequestHandler<GetOccupancyHistoryReportQuery, OccupancyHistoryReportDto>
    {
        private readonly IAccommodationReportRepository _reportRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<GetOccupancyHistoryReportHandler> _logger;

        public GetOccupancyHistoryReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<GetOccupancyHistoryReportHandler> logger)
        {
            _reportRepository = reportRepository;
            _accommodationRepository = accommodationRepository;
            _semesterRepository = semesterRepository;
            _auditService = auditService;
            _currentUser = currentUserService;
            _logger = logger;
        }

        public async Task<OccupancyHistoryReportDto> Handle(GetOccupancyHistoryReportQuery request, CancellationToken cancellationToken)
        {
            request.ValidatePeriod();
            if (!request.FromDate.HasValue || !request.ToDate.HasValue)
                throw new ValidationException("A From and To date are required for the Occupancy History report.");

            var filters = request.ToFilters();
            var (periodStart, periodEnd) = OccupancyDateRules.NormalizePeriod(filters.FromDate, filters.ToDate);

            var (page, distinctOccupants, distinctHouses) = await _reportRepository.GetOccupancyHistoryReportAsync(
                filters, periodStart, periodEnd, cancellationToken);

            var labels = await AccommodationReportSupport.ResolveFilterLabelsAsync(
                _accommodationRepository, _semesterRepository, filters, cancellationToken);

            var generatedBy = AccommodationReportSupport.ResolveGeneratedBy(_currentUser);
            var dto = new OccupancyHistoryReportDto
            {
                PeriodStart = filters.FromDate,
                PeriodEnd = filters.ToDate,
                DistinctOccupants = distinctOccupants,
                DistinctHouses = distinctHouses,
                Rows = page.Items,
                Pagination = AccommodationReportSupport.BuildPagination(filters, page.TotalCount)
            };

            AccommodationReportSupport.ApplyMeta(
                dto, "occupancy-history", "Occupancy History", filters, generatedBy, DateTime.UtcNow, labels);

            await AccommodationReportSupport.AuditAsync(_auditService, "occupancy-history", filters, generatedBy);

            _logger.LogInformation(
                "Occupancy history report generated: {Count} records, {Occupants} occupants, {Houses} houses, period {From:yyyy-MM-dd} to {To:yyyy-MM-dd}",
                page.TotalCount, distinctOccupants, distinctHouses, filters.FromDate, filters.ToDate);

            return dto;
        }
    }
}
