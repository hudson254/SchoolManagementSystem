using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class VacateHouseCommand : IRequest<bool>
    {
        public Guid HouseId { get; set; }
        public DateTime? VacatedDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class VacateHouseCommandValidator : AbstractValidator<VacateHouseCommand>
    {
        public VacateHouseCommandValidator()
        {
            RuleFor(x => x.HouseId).NotEmpty();
        }
    }

    public class VacateHouseHandler : IRequestHandler<VacateHouseCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<VacateHouseHandler> _logger;

        public VacateHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<VacateHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(VacateHouseCommand request, CancellationToken cancellationToken)
        {
            var house = await _repository.GetHouseByIdAsync(request.HouseId, cancellationToken);
            if (house == null)
                throw new SMS.Application.Exceptions.NotFoundException("House", request.HouseId);

            if (!house.IsOccupied)
                throw new SMS.Application.Exceptions.ValidationException($"House {house.HouseNumber} is not currently occupied");

            // Get active assignment and vacate it
            if (house.OccupantId.HasValue)
            {
                var occupantType = house.OccupantType ?? OccupantType.Student;
                var assignment = await _repository.GetAssignmentByOccupantAsync(house.OccupantId.Value, occupantType, cancellationToken);
                if (assignment != null)
                {
                    assignment.Status = "Vacated";
                    assignment.VacatedDate = request.VacatedDate ?? DateTime.UtcNow;
                    assignment.MoveOutDate = request.VacatedDate ?? DateTime.UtcNow;
                    assignment.Remarks = request.Remarks;
                    await _repository.UpdateAssignmentAsync(assignment, cancellationToken);
                }
            }

            // Update house status
            house.IsOccupied = false;
            house.OccupantId = null;
            house.OccupantType = null;
            house.Status = HouseStatus.Vacant;
            house.VacatedDate = request.VacatedDate ?? DateTime.UtcNow;
            house.SemesterId = null;
            await _repository.UpdateHouseAsync(house, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Vacate", "House",
                $"Vacated house {house.HouseNumber} (LaneId: {house.LaneId})");

            _logger.LogInformation("House {HouseNumber} vacated", house.HouseNumber);
            return true;
        }
    }
}
