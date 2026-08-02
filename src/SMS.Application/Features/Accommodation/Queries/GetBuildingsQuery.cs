using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetBuildingsQuery : IRequest<IEnumerable<BuildingDto>> { }

    public class GetBuildingsQueryHandler : IRequestHandler<GetBuildingsQuery, IEnumerable<BuildingDto>>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ILogger<GetBuildingsQueryHandler> _logger;

        public GetBuildingsQueryHandler(
            IAccommodationRepository accommodationRepository,
            ILogger<GetBuildingsQueryHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<BuildingDto>> Handle(GetBuildingsQuery request, CancellationToken cancellationToken)
        {
            var buildings = await _accommodationRepository.GetBuildingsAsync(cancellationToken);

            return buildings.Select(b => new BuildingDto
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address,
                TotalFloors = b.TotalFloors,
                HasElevator = b.HasElevator,
                Category = b.Category,
                IsActive = b.IsActive,
                TotalBlocks = b.Blocks?.Count ?? 0,
                TotalRooms = b.Blocks?.Sum(bl => bl.Rooms?.Count ?? 0) ?? 0
            }).ToList();
        }
    }
}
