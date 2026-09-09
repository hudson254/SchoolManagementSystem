using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to generate a report of all vacant houses, optionally filtered by lane.
    /// </summary>
    public class GetVacantHouseReportQuery : IRequest<VacantHouseReportDto>
    {
        public Guid? LaneId { get; set; }
    }

    public class GetVacantHouseReportHandler : IRequestHandler<GetVacantHouseReportQuery, VacantHouseReportDto>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetVacantHouseReportHandler> _logger;

        public GetVacantHouseReportHandler(
            IAccommodationRepository repository,
            ILogger<GetVacantHouseReportHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<VacantHouseReportDto> Handle(GetVacantHouseReportQuery request, CancellationToken cancellationToken)
        {
            // Use the repository to get houses by status "Vacant"
            var vacantHouses = await _repository.GetHousesByStatusAsync(HouseStatus.Vacant, cancellationToken);

            if (request.LaneId.HasValue)
            {
                vacantHouses = vacantHouses.Where(h => h.LaneId == request.LaneId.Value);
            }

            var houseDtos = vacantHouses.Select(h => AccommodationDtoMappings.ToHouseDto(h)).ToList();

            var report = new VacantHouseReportDto
            {
                TotalVacant = houseDtos.Count,
                VacantHouses = houseDtos
            };

            _logger.LogInformation("Vacant house report generated: {Count} vacant houses", report.TotalVacant);
            return report;
        }
    }
}

