using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to get overall occupancy statistics across all lanes.
    /// </summary>
    public class GetOccupancyStatisticsQuery : IRequest<OccupancyStatisticsDto>
    {
    }

    public class GetOccupancyStatisticsHandler : IRequestHandler<GetOccupancyStatisticsQuery, OccupancyStatisticsDto>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetOccupancyStatisticsHandler> _logger;

        public GetOccupancyStatisticsHandler(
            IAccommodationRepository repository,
            ILogger<GetOccupancyStatisticsHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<OccupancyStatisticsDto> Handle(GetOccupancyStatisticsQuery request, CancellationToken cancellationToken)
        {
            var lanes = await _repository.GetLanesAsync(cancellationToken);
            var overallStats = await _repository.GetOverallOccupancySummaryAsync(cancellationToken);

            var laneSummaries = new List<LaneOccupancyDto>();
            foreach (var lane in lanes)
            {
                var stats = await _repository.GetLaneOccupancySummaryAsync(lane.Id, cancellationToken);
                laneSummaries.Add(new LaneOccupancyDto
                {
                    LaneId = lane.Id,
                    LaneName = lane.LaneName,
                    TotalHouses = stats.Total,
                    Occupied = stats.Occupied,
                    Vacant = stats.Vacant,
                    Reserved = stats.Reserved,
                    Maintenance = stats.Maintenance,
                    Disabled = stats.Disabled,
                    OccupancyPercentage = stats.Total > 0
                        ? Math.Round((double)stats.Occupied / stats.Total * 100, 2)
                        : 0
                });
            }

            var statistics = new OccupancyStatisticsDto
            {
                TotalLanes = lanes.Count(),
                TotalHouses = overallStats.Total,
                OccupiedHouses = overallStats.Occupied,
                VacantHouses = overallStats.Vacant,
                ReservedHouses = laneSummaries.Sum(l => l.Reserved),
                MaintenanceHouses = overallStats.Maintenance,
                DisabledHouses = overallStats.Disabled,
                UnavailableHouses = overallStats.Total - overallStats.Occupied - overallStats.Vacant - overallStats.Maintenance,
                OccupancyPercentage = overallStats.Total > 0
                    ? Math.Round((double)overallStats.Occupied / overallStats.Total * 100, 2)
                    : 0,
                LaneSummaries = laneSummaries
            };

            _logger.LogInformation("Occupancy statistics generated: {TotalLanes} lanes, {TotalHouses} houses, {OccupancyPercentage}% occupied",
                statistics.TotalLanes, statistics.TotalHouses, statistics.OccupancyPercentage);
            return statistics;
        }
    }
}

