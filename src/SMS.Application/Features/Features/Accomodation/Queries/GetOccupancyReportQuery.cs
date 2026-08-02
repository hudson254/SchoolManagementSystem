using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetOccupancyReportQuery : IRequest<OccupancyReportDto>
    {
        public Guid? BuildingId { get; set; }
    }

    public class GetOccupancyReportQueryHandler : IRequestHandler<GetOccupancyReportQuery, OccupancyReportDto>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ILogger<GetOccupancyReportQueryHandler> _logger;

        public GetOccupancyReportQueryHandler(
            IAccommodationRepository accommodationRepository,
            ILogger<GetOccupancyReportQueryHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _logger = logger;
        }

        public async Task<OccupancyReportDto> Handle(GetOccupancyReportQuery request, CancellationToken cancellationToken)
        {
            var allRooms = await _accommodationRepository.GetRoomsByBuildingAsync(request.BuildingId, cancellationToken);
            var occupiedRooms = allRooms.Where(r => r.IsOccupied).ToList();
            var availableRooms = allRooms.Where(r => r.IsAvailable && !r.IsOccupied).ToList();
            var maintenanceRooms = allRooms.Where(r => r.Status == "Maintenance").ToList();

            var totalRooms = allRooms.Count();
            var occupiedCount = occupiedRooms.Count();
            var availableCount = availableRooms.Count();
            var maintenanceCount = maintenanceRooms.Count();

            var occupancyRate = totalRooms > 0 ? (decimal)occupiedCount / totalRooms * 100 : 0;

            var buildingOccupancy = new List<BuildingOccupancyDto>();

            var buildingGroups = allRooms
                .GroupBy(r => r.Block?.Building?.Name ?? "Unknown")
                .Select(g => new
                {
                    BuildingName = g.Key,
                    Rooms = g.ToList()
                });

            foreach (var group in buildingGroups)
            {
                var total = group.Rooms.Count;
                var occupied = group.Rooms.Count(r => r.IsOccupied);
                var available = group.Rooms.Count(r => r.IsAvailable && !r.IsOccupied);

                buildingOccupancy.Add(new BuildingOccupancyDto
                {
                    BuildingName = group.BuildingName,
                    TotalRooms = total,
                    OccupiedRooms = occupied,
                    AvailableRooms = available,
                    OccupancyRate = total > 0 ? (decimal)occupied / total * 100 : 0
                });
            }

            return new OccupancyReportDto
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedCount,
                AvailableRooms = availableCount,
                MaintenanceRooms = maintenanceCount,
                OccupancyRate = occupancyRate,
                BuildingOccupancy = buildingOccupancy
            };
        }
    }
}