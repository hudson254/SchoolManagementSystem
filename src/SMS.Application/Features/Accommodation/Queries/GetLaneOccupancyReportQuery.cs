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
                OccupancyPercentage = stats.Total > 0
                    ? Math.Round((double)stats.Occupied / stats.Total * 100, 2)
                    : 0,
                Houses = houses.Select(h => new HouseDto
                {
                    Id = h.Id,
                    LaneId = h.LaneId,
                    LaneName = lane.LaneName,
                    HouseNumber = h.HouseNumber,
                    HouseNumberNumeric = h.HouseNumberNumeric,
                    Status = h.Status,
                    IsOccupied = h.IsOccupied,
                    IsEnabled = h.IsEnabled,
                    IsAvailable = h.IsAvailable,
                    OccupantId = h.OccupantId,
                    OccupantName = h.Occupant != null ? $"{h.Occupant.FirstName} {h.Occupant.LastName}" : null,
                    StudentNumber = h.Occupant?.StudentNumber,
                    Notes = h.Notes,
                    OccupiedDate = h.OccupiedDate,
                    CreatedDate = h.CreatedDate.GetValueOrDefault(),
                    UpdatedDate = h.ModifiedDate
                }).ToList()
            };

            _logger.LogInformation("Lane occupancy report generated for lane '{LaneName}' ({LaneId})",
                lane.LaneName, request.LaneId);
            return report;
        }
    }
}

