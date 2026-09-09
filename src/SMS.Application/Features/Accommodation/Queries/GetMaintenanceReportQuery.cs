using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Queries
{
    /// <summary>
    /// Query to generate a report of all houses currently under maintenance.
    /// </summary>
    public class GetMaintenanceReportQuery : IRequest<MaintenanceReportDto>
    {
        public Guid? LaneId { get; set; }
    }

    public class GetMaintenanceReportHandler : IRequestHandler<GetMaintenanceReportQuery, MaintenanceReportDto>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ILogger<GetMaintenanceReportHandler> _logger;

        public GetMaintenanceReportHandler(
            IAccommodationRepository repository,
            ILogger<GetMaintenanceReportHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<MaintenanceReportDto> Handle(GetMaintenanceReportQuery request, CancellationToken cancellationToken)
        {
            var maintenanceHouses = await _repository.GetHousesUnderMaintenanceAsync(cancellationToken);

            if (request.LaneId.HasValue)
            {
                maintenanceHouses = maintenanceHouses.Where(h => h.LaneId == request.LaneId.Value);
            }

            var houseDtos = maintenanceHouses.Select(h => AccommodationDtoMappings.ToHouseDto(h)).ToList();

            var report = new MaintenanceReportDto
            {
                TotalUnderMaintenance = houseDtos.Count,
                HousesUnderMaintenance = houseDtos
            };

            _logger.LogInformation("Maintenance report generated: {Count} houses under maintenance", report.TotalUnderMaintenance);
            return report;
        }
    }
}

