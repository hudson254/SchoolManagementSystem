using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// H. House Utilization Summary — capacity, occupancy and utilization
    /// percentages for the filtered set of houses. Read-only; the underlying
    /// occupancy records are never modified.
    /// </summary>
    public class GetHouseUtilizationSummaryReportQuery : AccommodationReportQueryBase, IRequest<HouseUtilizationSummaryReportDto>
    {
    }

    public class GetHouseUtilizationSummaryReportHandler
        : IRequestHandler<GetHouseUtilizationSummaryReportQuery, HouseUtilizationSummaryReportDto>
    {
        private readonly IAccommodationReportRepository _reportRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<GetHouseUtilizationSummaryReportHandler> _logger;

        public GetHouseUtilizationSummaryReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<GetHouseUtilizationSummaryReportHandler> logger)
        {
            _reportRepository = reportRepository;
            _accommodationRepository = accommodationRepository;
            _semesterRepository = semesterRepository;
            _auditService = auditService;
            _currentUser = currentUserService;
            _logger = logger;
        }

        public async Task<HouseUtilizationSummaryReportDto> Handle(
            GetHouseUtilizationSummaryReportQuery request,
            CancellationToken cancellationToken)
        {
            request.ValidatePeriod();

            var filters = request.ToFilters();
            var summary = await _reportRepository.GetUtilizationSummaryAsync(filters, cancellationToken);

            var labels = await AccommodationReportSupport.ResolveFilterLabelsAsync(
                _accommodationRepository, _semesterRepository, filters, cancellationToken);

            var generatedBy = AccommodationReportSupport.ResolveGeneratedBy(_currentUser);
            var dto = new HouseUtilizationSummaryReportDto { Summary = summary };

            AccommodationReportSupport.ApplyMeta(
                dto, "utilization-summary", "House Utilization Summary", filters, generatedBy, DateTime.UtcNow, labels);

            await AccommodationReportSupport.AuditAsync(_auditService, "utilization-summary", filters, generatedBy);

            _logger.LogInformation(
                "Utilization summary generated: {Houses} houses, {Occupied}/{Capacity} spaces ({Percent}%)",
                summary.TotalHouses, summary.OccupiedSpaces, summary.TotalCapacity, summary.OccupancyPercentage);

            return dto;
        }
    }
}
