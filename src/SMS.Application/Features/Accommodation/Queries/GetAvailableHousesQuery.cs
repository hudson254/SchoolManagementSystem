using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to get all available (vacant, enabled, not under maintenance) houses.
    /// </summary>
    public class GetAvailableHousesQuery : IRequest<IEnumerable<HouseDto>>
    {
        public Guid? LaneId { get; set; }
    }

    public class GetAvailableHousesHandler : IRequestHandler<GetAvailableHousesQuery, IEnumerable<HouseDto>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetAvailableHousesHandler> _logger;

        public GetAvailableHousesHandler(
            IAccommodationRepository repository,
            ILogger<GetAvailableHousesHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<HouseDto>> Handle(GetAvailableHousesQuery request, CancellationToken cancellationToken)
        {
            var houses = await _repository.GetAvailableHousesAsync(request.LaneId, cancellationToken);

            var houseDtos = houses.Select(h => new HouseDto
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
                StudentNumber = h.Occupant?.StudentNumber,
                SemesterId = h.SemesterId,
                Notes = h.Notes,
                OccupiedDate = h.OccupiedDate,
                CreatedDate = h.CreatedDate.GetValueOrDefault(),
                UpdatedDate = h.ModifiedDate
            });

            _logger.LogInformation("Retrieved {Count} available houses", houseDtos.Count());
            return houseDtos;
        }
    }
}
