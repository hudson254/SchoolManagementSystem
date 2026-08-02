using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to get a single house by its ID.
    /// </summary>
    public class GetHouseQuery : IRequest<HouseDto>
    {
        public Guid Id { get; set; }
    }

    public class GetHouseHandler : IRequestHandler<GetHouseQuery, HouseDto>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetHouseHandler> _logger;

        public GetHouseHandler(
            IAccommodationRepository repository,
            ILogger<GetHouseHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<HouseDto> Handle(GetHouseQuery request, CancellationToken cancellationToken)
        {
            var house = await _repository.GetHouseByIdAsync(request.Id, cancellationToken);
            if (house == null)
                throw new NotFoundException("House", request.Id);

            var dto = new HouseDto
            {
                Id = house.Id,
                LaneId = house.LaneId,
                LaneName = house.Lane?.LaneName ?? string.Empty,
                HouseNumber = house.HouseNumber,
                HouseNumberNumeric = house.HouseNumberNumeric,
                Status = house.Status,
                IsOccupied = house.IsOccupied,
                IsEnabled = house.IsEnabled,
                IsAvailable = house.IsAvailable,
                OccupantId = house.OccupantId,
                OccupantName = house.Occupant != null ? $"{house.Occupant.FirstName} {house.Occupant.LastName}" : null,
                StudentNumber = house.Occupant?.StudentNumber,
                SemesterId = house.SemesterId,
                Notes = house.Notes,
                OccupiedDate = house.OccupiedDate,
                CreatedDate = house.CreatedDate.GetValueOrDefault(),
                UpdatedDate = house.ModifiedDate
            };

            _logger.LogInformation("Retrieved house: {HouseNumber} ({HouseId})", house.HouseNumber, house.Id);
            return dto;
        }
    }
}
