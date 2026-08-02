using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetLanesQuery : IRequest<IEnumerable<LaneDto>>
    {
        public string? SearchTerm { get; set; }
    }

    public class GetLanesHandler : IRequestHandler<GetLanesQuery, IEnumerable<LaneDto>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetLanesHandler> _logger;

        public GetLanesHandler(
            IAccommodationRepository repository,
            ILogger<GetLanesHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<LaneDto>> Handle(GetLanesQuery request, CancellationToken cancellationToken)
        {
            var lanes = await _repository.GetLanesAsync(cancellationToken);
            var laneDtos = new List<LaneDto>();

            foreach (var lane in lanes)
            {
                var stats = await _repository.GetLaneOccupancySummaryAsync(lane.Id, cancellationToken);
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
                    UpdatedDate = lane.ModifiedDate ?? lane.CreatedDate
                };
                laneDtos.Add(dto);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                laneDtos = laneDtos.Where(l =>
                    l.LaneName.ToLower().Contains(search) ||
                    (l.Description != null && l.Description.ToLower().Contains(search))
                ).ToList();
            }

            _logger.LogInformation("Retrieved {Count} lanes", laneDtos.Count);
            return laneDtos;
        }
    }
}
