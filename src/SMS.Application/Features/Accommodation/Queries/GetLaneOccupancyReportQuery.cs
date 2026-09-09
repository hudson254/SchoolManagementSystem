using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to generate a lane occupancy report with detailed house information.
    /// </summary>
    public class GetLaneOccupancyReportQuery : IRequest<LaneOccupancyReportDto>
    {
        public Guid LaneId { get; set; }
    }

    public class GetLaneOccupancyReportHandler : IRequestHandler<GetLaneOccupancyReportQuery, LaneOccupancyReportDto>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetLaneOccupancyReportHandler> _logger;

        public GetLaneOccupancyReportHandler(
            IAccommodationRepository repository,
            ILogger<GetLaneOccupancyReportHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<LaneOccupancyReportDto> Handle(GetLaneOccupancyReportQuery request, CancellationToken cancellationToken)
        {
            var lane = await _repository.GetLaneByIdAsync(request.LaneId, cancellationToken);
            if (lane == null)
                throw new NotFoundException("Lane", request.LaneId);

            var houses = await _repository.GetHousesByLaneAsync(request.LaneId, cancellationToken);
            var stats = await _repository.GetLaneOccupancySummaryAsync(request.LaneId, cancellationToken);

            var totalCapacity = houses.Sum(h => h.Capacity);
            var occupants = houses.Sum(h => h.OccupiedCount);

            var report = new LaneOccupancyReportDto
            {
                LaneId = lane.Id,
                LaneName = lane.LaneName,
                TotalHouses = stats.Total,
                Occupied = stats.Occupied,
                Vacant = stats.Vacant,
                Reserved = stats.Reserved,
                Maintenance = stats.Maintenance,
                Disabled = stats.Disabled,
                Unavailable = houses.Count(h => h.Status == Domain.Entities.HouseStatus.Unavailable),
                OccupancyPercentage = totalCapacity > 0
                    ? Math.Round((double)occupants / totalCapacity * 100, 2)
                    : 0,
                TotalCapacity = totalCapacity,
                Occupants = occupants,
                Houses = houses.Select(h => AccommodationDtoMappings.ToHouseDto(h)).ToList()
            };

            _logger.LogInformation("Lane occupancy report generated for lane '{LaneName}' ({LaneId})",
                lane.LaneName, request.LaneId);
            return report;
        }
    }
}

