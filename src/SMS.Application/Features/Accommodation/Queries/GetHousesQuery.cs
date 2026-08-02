using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetHousesQuery : IRequest<IEnumerable<HouseDto>>
    {
        public Guid? LaneId { get; set; }
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
    }

    public class GetHousesHandler : IRequestHandler<GetHousesQuery, IEnumerable<HouseDto>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetHousesHandler> _logger;

        public GetHousesHandler(
            IAccommodationRepository repository,
            ILogger<GetHousesHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<HouseDto>> Handle(GetHousesQuery request, CancellationToken cancellationToken)
        {
            var (items, _) = await _repository.GetHousesPagedAsync(1, int.MaxValue, request.LaneId, request.SearchTerm, request.Status, cancellationToken);
            var houseDtos = items.Select(h => new HouseDto
            {
                Id = h.Id,
                LaneId = h.LaneId,
                LaneName = h.Lane?.LaneName ?? string.Empty,
                HouseNumber = h.HouseNumber,
                HouseNumberNumeric = h.HouseNumberNumeric,
                Status = h.Status,
                IsOccupied = h.IsOccupied,
                IsEnabled = h.IsEnabled,
                IsAvailable = h.IsAvailable,
                OccupantId = h.OccupantId,
                OccupantName = h.Occupant != null ? $"{h.Occupant.FirstName} {h.Occupant.LastName}" : null,
                SemesterId = h.SemesterId,
                Notes = h.Notes,
                OccupiedDate = h.OccupiedDate,
                CreatedDate = h.CreatedDate.GetValueOrDefault(),
                UpdatedDate = h.ModifiedDate
            });

            _logger.LogInformation("Retrieved {Count} houses", houseDtos.Count());
            return houseDtos;
        }
    }
}
