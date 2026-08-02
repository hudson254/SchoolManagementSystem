using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetLaneQuery : IRequest<LaneDto>
    {
        public Guid Id { get; set; }
    }

    public class GetLaneHandler : IRequestHandler<GetLaneQuery, LaneDto>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetLaneHandler> _logger;

        public GetLaneHandler(
            IAccommodationRepository repository,
            ILogger<GetLaneHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<LaneDto> Handle(GetLaneQuery request, CancellationToken cancellationToken)
        {
            var lane = await _repository.GetLaneByIdAsync(request.Id, cancellationToken);
            if (lane == null)
                throw new SMS.Application.Exceptions.NotFoundException("Lane", request.Id);

            var stats = await _repository.GetLaneOccupancySummaryAsync(request.Id, cancellationToken);

            var dto = new LaneDto
            {
                Id = lane.Id,
                LaneName = lane.LaneName,
                Description = lane.Description,
                IsActive = lane.IsActive,
                TotalHouses = stats.Total,
                OccupiedHouses = stats.Occupied,
                VacantHouses = stats.Vacant,
                MaintenanceCount = stats.Maintenance,
                NumberingFormat = lane.NumberingFormat,
                StartingHouseNumber = lane.StartingHouseNumber,
                CreatedDate = lane.CreatedDate.GetValueOrDefault(),
                UpdatedDate = lane.ModifiedDate
            };

            return dto;
        }
    }
}
