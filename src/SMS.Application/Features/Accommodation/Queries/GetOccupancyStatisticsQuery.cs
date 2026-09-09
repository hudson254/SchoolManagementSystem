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

            var allActiveAssignments = (await _repository.GetActiveAssignmentsAsync(null, null, null, cancellationToken)).ToList();
            var totalOccupants = allActiveAssignments.Count;
            var studentOccupants = allActiveAssignments.Count(a => a.OccupantType == SMS.Domain.Enums.OccupantType.Student);
            var lecturerOccupants = allActiveAssignments.Count(a => a.OccupantType == SMS.Domain.Enums.OccupantType.Lecturer);

            var laneSummaries = new List<LaneOccupancyDto>();
            foreach (var lane in lanes)
            {
                var stats = await _repository.GetLaneOccupancySummaryAsync(lane.Id, cancellationToken);
                var laneHouses = await _repository.GetHousesByLaneAsync(lane.Id, cancellationToken);
                var laneAssignments = allActiveAssignments
                    .Where(a => a.House != null && a.House.LaneId == lane.Id)
                    .ToList();

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
                    TotalCapacity = laneHouses.Sum(h => h.Capacity),
                    Occupants = laneAssignments.Count,
                    StudentOccupants = laneAssignments.Count(a => a.OccupantType == SMS.Domain.Enums.OccupantType.Student),
                    LecturerOccupants = laneAssignments.Count(a => a.OccupantType == SMS.Domain.Enums.OccupantType.Lecturer),
                    OccupancyPercentage = laneHouses.Sum(h => h.Capacity) > 0
                        ? Math.Round((double)laneAssignments.Count / laneHouses.Sum(h => h.Capacity) * 100, 2)
                        : 0
                });
            }

            var totalCapacity = (await _repository.GetHousesPagedAsync(1, int.MaxValue, null, null, null, cancellationToken)).Items.Sum(h => h.Capacity);

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
                TotalCapacity = totalCapacity,
                TotalOccupants = totalOccupants,
                StudentOccupants = studentOccupants,
                LecturerOccupants = lecturerOccupants,
                OccupancyPercentage = totalCapacity > 0
                    ? Math.Round((double)totalOccupants / totalCapacity * 100, 2)
                    : 0,
                LaneSummaries = laneSummaries
            };

            _logger.LogInformation("Occupancy statistics generated: {TotalLanes} lanes, {TotalHouses} houses, {TotalOccupants} occupants / {TotalCapacity} capacity ({OccupancyPercentage}%)",
                statistics.TotalLanes, statistics.TotalHouses, statistics.TotalOccupants, statistics.TotalCapacity, statistics.OccupancyPercentage);
            return statistics;
        }
    }
}

