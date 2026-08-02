using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetAvailableRoomsQuery : IRequest<IEnumerable<RoomDto>>
    {
        public Guid? BuildingId { get; set; }
        public Guid? BlockId { get; set; }
        public string? RoomType { get; set; }
    }

    public class GetAvailableRoomsQueryHandler : IRequestHandler<GetAvailableRoomsQuery, IEnumerable<RoomDto>>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ILogger<GetAvailableRoomsQueryHandler> _logger;

        public GetAvailableRoomsQueryHandler(
            IAccommodationRepository accommodationRepository,
            ILogger<GetAvailableRoomsQueryHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<RoomDto>> Handle(GetAvailableRoomsQuery request, CancellationToken cancellationToken)
        {
            var rooms = await _accommodationRepository.GetAvailableRoomsAsync();

            return rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                BlockId = r.BlockId,
                Capacity = r.Capacity,
                RoomType = r.RoomType,
                PricePerSemester = r.PricePerSemester,
                Facilities = r.Facilities,
                IsAvailable = r.IsAvailable,
                IsOccupied = r.IsOccupied,
                Status = r.Status ?? "Available",
                BlockName = r.Block?.Name ?? string.Empty,
                BuildingName = r.Block?.Building ?? string.Empty
            });
        }
    }
}





