using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    /// <summary>
    /// Command to reassign a student from one house to another.
    /// </summary>
    public class ReassignHouseCommand : IRequest<bool>
    {
        public Guid StudentId { get; set; }
        public Guid NewHouseId { get; set; }
        public string? Remarks { get; set; }
    }

    public class ReassignHouseCommandValidator : AbstractValidator<ReassignHouseCommand>
    {
        public ReassignHouseCommandValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("Student ID is required");
            RuleFor(x => x.NewHouseId).NotEmpty().WithMessage("New house ID is required");
        }
    }

    public class ReassignHouseHandler : IRequestHandler<ReassignHouseCommand, bool>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ReassignHouseHandler> _logger;

        public ReassignHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<ReassignHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<bool> Handle(ReassignHouseCommand request, CancellationToken cancellationToken)
        {
            // Get the student's current assignment
            var currentAssignment = await _repository.GetAssignmentByStudentAsync(request.StudentId, cancellationToken);
            if (currentAssignment == null || currentAssignment.Status != "Active")
                throw new NotFoundException("Active accommodation assignment", request.StudentId);

            // Get the current house
            var currentHouse = await _repository.GetHouseByIdAsync(currentAssignment.HouseId, cancellationToken);
            if (currentHouse == null)
                throw new NotFoundException("House", currentAssignment.HouseId);

            // Get the new house
            var newHouse = await _repository.GetHouseByIdAsync(request.NewHouseId, cancellationToken);
            if (newHouse == null)
                throw new NotFoundException("House", request.NewHouseId);

            // Check if new house is available
            if (newHouse.IsOccupied || !newHouse.IsAvailable || !newHouse.IsEnabled)
                throw new BusinessRuleException("Cannot reassign",
                    $"House {newHouse.HouseNumber} is not available for assignment");

            // Check if new house is in maintenance or disabled
            if (newHouse.Status == HouseStatus.Maintenance || newHouse.Status == HouseStatus.Disabled || newHouse.Status == HouseStatus.Unavailable)
                throw new BusinessRuleException("Cannot reassign",
                    $"House {newHouse.HouseNumber} is {newHouse.Status.ToLower()} and cannot be assigned");

            // Vacate the current house
            currentHouse.IsOccupied = false;
            currentHouse.OccupantId = null;
            currentHouse.Status = HouseStatus.Vacant;
            currentHouse.VacatedDate = DateTime.UtcNow;
            currentHouse.SemesterId = null;
            await _repository.UpdateHouseAsync(currentHouse, cancellationToken);

            // Update the current assignment to completed
            currentAssignment.Status = "Completed";
            currentAssignment.VacatedDate = DateTime.UtcNow;
            currentAssignment.MoveOutDate = DateTime.UtcNow;
            currentAssignment.Remarks = request.Remarks ?? $"Reassigned from house {currentHouse.HouseNumber}";
            await _repository.UpdateAssignmentAsync(currentAssignment, cancellationToken);

            // Assign the new house
            var newAssignment = new AccommodationAssignment
            {
                StudentId = request.StudentId,
                HouseId = request.NewHouseId,
                LaneId = newHouse.LaneId,
                SemesterId = currentAssignment.SemesterId,
                AssignedDate = DateTime.UtcNow,
                AssignmentDate = DateTime.UtcNow,
                MoveInDate = DateTime.UtcNow,
                Status = "Active",
                Remarks = request.Remarks
            };

            await _repository.AddAssignmentAsync(newAssignment, cancellationToken);

            // Update the new house status
            newHouse.IsOccupied = true;
            newHouse.OccupantId = request.StudentId;
            newHouse.Status = HouseStatus.Occupied;
            newHouse.OccupiedDate = DateTime.UtcNow;
            await _repository.UpdateHouseAsync(newHouse, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Reassign", "House",
                $"Reassigned student {request.StudentId} from house {currentHouse.HouseNumber} to house {newHouse.HouseNumber}");

            _logger.LogInformation("Student {StudentId} reassigned from house {OldHouse} to house {NewHouse}",
                request.StudentId, currentHouse.HouseNumber, newHouse.HouseNumber);

            return true;
        }
    }
}
