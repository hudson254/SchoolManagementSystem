using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ICurrentUserService = SMS.Domain.Interfaces.ICurrentUserService;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// E. House Occupancy History — the complete occupancy history of one
    /// selected house, oldest first, including current and former occupants.
    /// </summary>
    public class GetHouseOccupancyHistoryReportQuery : AccommodationReportQueryBase, IRequest<HouseOccupancyHistoryReportDto>
    {
    }

    public class GetHouseOccupancyHistoryReportHandler : IRequestHandler<GetHouseOccupancyHistoryReportQuery, HouseOccupancyHistoryReportDto>
    {
        private readonly IAccommodationReportRepository _reportRepository;
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<GetHouseOccupancyHistoryReportHandler> _logger;

        public GetHouseOccupancyHistoryReportHandler(
            IAccommodationReportRepository reportRepository,
            IAccommodationRepository accommodationRepository,
            ISemesterRepository semesterRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<GetHouseOccupancyHistoryReportHandler> logger)
        {
            _reportRepository = reportRepository;
            _accommodationRepository = accommodationRepository;
            _semesterRepository = semesterRepository;
            _auditService = auditService;
            _currentUser = currentUserService;
            _logger = logger;
        }

        public async Task<HouseOccupancyHistoryReportDto> Handle(
            GetHouseOccupancyHistoryReportQuery request,
            CancellationToken cancellationToken)
        {
            request.ValidatePeriod();
            if (!request.HouseId.HasValue || request.HouseId.Value == Guid.Empty)
                throw new ValidationException("A house must be selected for the House History report.");

            var houseId = request.HouseId.Value;
            var house = await _accommodationRepository.GetHouseByIdAsync(houseId, cancellationToken);
            if (house == null)
                throw new NotFoundException("House", houseId);

            var filters = request.ToFilters();
            var (stays, currentOccupants) = await _reportRepository.GetHouseHistoryReportAsync(
                houseId, filters, cancellationToken);

            var labels = await AccommodationReportSupport.ResolveFilterLabelsAsync(
                _accommodationRepository, _semesterRepository, filters, cancellationToken);

            var generatedBy = AccommodationReportSupport.ResolveGeneratedBy(_currentUser);
            var dto = new HouseOccupancyHistoryReportDto
            {
                HouseId = house.Id,
                HouseNumber = house.HouseNumber,
                HouseName = house.HouseName,
                LaneName = house.Lane?.LaneName ?? string.Empty,
                HouseStatus = house.Status,
                Capacity = house.Capacity,
                CurrentOccupants = currentOccupants,
                TotalStays = stays.TotalCount,
                Rows = stays.Items,
                Pagination = AccommodationReportSupport.BuildPagination(filters, stays.TotalCount)
            };

            AccommodationReportSupport.ApplyMeta(
                dto, "house-history", $"House Occupancy History — {house.HouseNumber}",
                filters, generatedBy, DateTime.UtcNow, labels);

            await AccommodationReportSupport.AuditAsync(_auditService, "house-history", filters, generatedBy);

            _logger.LogInformation(
                "House history report generated for {HouseNumber} (HouseId: {HouseId}): {Count} stays",
                house.HouseNumber, houseId, stays.TotalCount);

            return dto;
        }
    }
}
