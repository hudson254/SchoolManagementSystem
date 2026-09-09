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

            var dto = AccommodationDtoMappings.ToHouseDto(house);

            _logger.LogInformation("Retrieved house: {HouseNumber} ({HouseId})", house.HouseNumber, house.Id);
            return dto;
        }
    }
}
