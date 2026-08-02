using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetAccommodationDashboardQuery : IRequest<AccommodationDashboardDto>
    {
    }

    public class GetAccommodationDashboardHandler : IRequestHandler<GetAccommodationDashboardQuery, AccommodationDashboardDto>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetAccommodationDashboardHandler> _logger;

        public GetAccommodationDashboardHandler(
            IAccommodationRepository repository,
            ILogger<GetAccommodationDashboardHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<AccommodationDashboardDto> Handle(GetAccommodationDashboardQuery request, CancellationToken cancellationToken)
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

            var dashboard = new AccommodationDashboardDto
            {
                TotalLanes = lanes.Count(),
                TotalHouses = overallStats.Total,
                OccupiedHouses = overallStats.Occupied,
                VacantHouses = overallStats.Vacant,
                MaintenanceCount = overallStats.Maintenance,
                DisabledCount = overallStats.Disabled,
                OccupancyPercentage = overallStats.Total > 0
                    ? Math.Round((double)overallStats.Occupied / overallStats.Total * 100, 2)
                    : 0,
                LaneSummaries = laneSummaries
            };

            _logger.LogInformation("Accommodation dashboard generated: {TotalLanes} lanes, {TotalHouses} houses, {OccupancyPercentage}% occupied",
                dashboard.TotalLanes, dashboard.TotalHouses, dashboard.OccupancyPercentage);

            return dashboard;
        }
    }
}
