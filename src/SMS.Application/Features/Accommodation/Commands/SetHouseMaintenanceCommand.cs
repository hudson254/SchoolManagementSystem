using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Command to mark a house as under maintenance or restore it from maintenance.
    /// </summary>
    public class SetHouseMaintenanceCommand : IRequest<bool>
    {
        public Guid HouseId { get; set; }
        public bool IsUnderMaintenance { get; set; }
        public string? Notes { get; set; }
    }

    public class SetHouseMaintenanceCommandValidator : AbstractValidator<SetHouseMaintenanceCommand>
    {
        public SetHouseMaintenanceCommandValidator()
        {
            RuleFor(x => x.HouseId).NotEmpty();
        }
    }

    public class SetHouseMaintenanceHandler : IRequestHandler<SetHouseMaintenanceCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<SetHouseMaintenanceHandler> _logger;

        public SetHouseMaintenanceHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<SetHouseMaintenanceHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(SetHouseMaintenanceCommand request, CancellationToken cancellationToken)
        {
            var house = await _repository.GetHouseByIdAsync(request.HouseId, cancellationToken);
            if (house == null)
                throw new NotFoundException("House", request.HouseId);

            var oldStatus = house.Status;

            if (request.IsUnderMaintenance)
            {
                if (house.IsOccupied)
                    throw new BusinessRuleException("Cannot set maintenance",
                        $"House {house.HouseNumber} is currently occupied. Vacate the house first.");

                house.Status = HouseStatus.Maintenance;
                house.IsAvailable = false;
            }
            else
            {
                // Restore from maintenance - set back to vacant if not occupied
                if (!house.IsOccupied)
                {
                    house.Status = HouseStatus.Vacant;
                    house.IsAvailable = true;
                }
            }

            if (!string.IsNullOrEmpty(request.Notes))
                house.Notes = request.Notes;

            await _repository.UpdateHouseAsync(house, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogDataChangeAsync("House", house.Id.ToString(), "MaintenanceStatusChange",
                $"Old: {oldStatus}, New: {house.Status}, Notes: {request.Notes ?? "N/A"}");

            _logger.LogInformation("House {HouseNumber} maintenance status changed to {Status}",
                house.HouseNumber, house.Status);
            return true;
        }
    }
}
