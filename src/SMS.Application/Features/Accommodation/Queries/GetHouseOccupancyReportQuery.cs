using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to generate a house occupancy report across all lanes.
    /// </summary>
    public class GetHouseOccupancyReportQuery : IRequest<IEnumerable<HouseOccupancyReportDto>>
    {
        public Guid? LaneId { get; set; }
        public string? Status { get; set; }
    }

    public class GetHouseOccupancyReportHandler : IRequestHandler<GetHouseOccupancyReportQuery, IEnumerable<HouseOccupancyReportDto>>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetHouseOccupancyReportHandler> _logger;

        public GetHouseOccupancyReportHandler(
            IAccommodationRepository repository,
            ILogger<GetHouseOccupancyReportHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<HouseOccupancyReportDto>> Handle(GetHouseOccupancyReportQuery request, CancellationToken cancellationToken)
        {
            var (items, _) = await _repository.GetHousesPagedAsync(1, int.MaxValue, request.LaneId, null, request.Status, cancellationToken);

            var report = items.Select(h => new HouseOccupancyReportDto
            {
                HouseId = h.Id,
                HouseNumber = h.HouseNumber,
                LaneName = h.Lane?.LaneName ?? string.Empty,
                Status = h.Status,
                IsOccupied = h.IsOccupied,
                OccupantName = h.Occupant != null ? $"{h.Occupant.FirstName} {h.Occupant.LastName}" : null,
                StudentNumber = h.Occupant?.StudentNumber,
                OccupiedDate = h.OccupiedDate,
                VacatedDate = h.VacatedDate,
                Notes = h.Notes
            });

            _logger.LogInformation("House occupancy report generated with {Count} houses", report.Count());
            return report;
        }
    }
}

