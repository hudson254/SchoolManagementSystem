using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    public class GetRoomsQuery : IRequest<PagedResult<RoomDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public Guid? BuildingId { get; set; }
        public Guid? BlockId { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsOccupied { get; set; }
        public string? RoomType { get; set; }
        public string SortBy { get; set; } = "RoomNumber";
        public bool SortDescending { get; set; } = false;
    }

    public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, PagedResult<RoomDto>>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly ILogger<GetRoomsQueryHandler> _logger;

        public GetRoomsQueryHandler(
            IAccommodationRepository accommodationRepository,
            ILogger<GetRoomsQueryHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _logger = logger;
        }

        public async Task<PagedResult<RoomDto>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
        {
            var rooms = await _accommodationRepository.GetRoomsAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.BuildingId,
                request.BlockId,
                request.IsAvailable,
                request.IsOccupied,
                request.RoomType,
                request.SortBy,
                request.SortDescending,
                cancellationToken);

            var totalCount = await _accommodationRepository.CountRoomsAsync(
                request.SearchTerm,
                request.BuildingId,
                request.BlockId,
                request.IsAvailable,
                request.IsOccupied,
                request.RoomType,
                cancellationToken);

            var dtos = rooms.Select(r => new RoomDto
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
                BuildingName = r.Block?.Building?.Name ?? string.Empty,
                CurrentOccupant = r.CurrentAssignment?.Student?.User.FullName
            }).ToList();

            return new PagedResult<RoomDto>
            {
                Items = dtos,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}