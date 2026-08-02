using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetBuildingQuery : IRequest<BuildingDetailsDto>
    {
        public Guid BuildingId { get; set; }
    }

    public class GetBuildingQueryHandler : IRequestHandler<GetBuildingQuery, BuildingDetailsDto>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ILogger<GetBuildingQueryHandler> _logger;

        public GetBuildingQueryHandler(
            IAccommodationRepository accommodationRepository,
            ILogger<GetBuildingQueryHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _logger = logger;
        }

        public async Task<BuildingDetailsDto> Handle(GetBuildingQuery request, CancellationToken cancellationToken)
        {
            var building = await _accommodationRepository.GetBuildingByIdAsync(request.BuildingId, cancellationToken);
            if (building == null)
                throw new NotFoundException("Building", request.BuildingId);

            var totalRooms = building.Blocks?.Sum(bl => bl.Rooms?.Count ?? 0) ?? 0;
            var occupiedRooms = building.Blocks?.Sum(bl => bl.Rooms?.Count(r => r.OccupiedCount > 0) ?? 0) ?? 0;
            var availableRooms = totalRooms - occupiedRooms;

            return new BuildingDetailsDto
            {
                Id = building.Id,
                Name = building.Name,
                Address = building.Address,
                TotalFloors = building.TotalFloors,
                HasElevator = building.HasElevator,
                Category = building.Category,
                IsActive = building.IsActive,
                TotalBlocks = building.Blocks?.Count ?? 0,
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                AvailableRooms = availableRooms,
                OccupancyRate = totalRooms > 0 ? Math.Round((decimal)occupiedRooms / totalRooms * 100, 2) : 0,
                Blocks = (building.Blocks ?? new List<Domain.Entities.Block>()).Select(bl => new BlockDto
                {
                    Id = bl.Id,
                    Name = bl.Name,
                    BuildingId = building.Id,
                    FloorNumber = 0, // Block doesn't have FloorNumber in domain model
                    TotalRooms = bl.Rooms?.Count ?? 0,
                    Category = building.Category,
                    IsActive = bl.IsActive,
                    OccupiedRooms = bl.Rooms?.Count(r => r.OccupiedCount > 0) ?? 0,
                    AvailableRooms = (bl.Rooms?.Count ?? 0) - (bl.Rooms?.Count(r => r.OccupiedCount > 0) ?? 0)
                }).ToList()
            };
        }
    }
}
