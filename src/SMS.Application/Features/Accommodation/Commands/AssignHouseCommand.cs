using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class AssignHouseCommand : IRequest<Guid>
    {
        public Guid? StudentId { get; set; }
        public Guid? LecturerId { get; set; }
        public OccupantType OccupantType { get; set; } = OccupantType.Student;
        public Guid HouseId { get; set; }
        public Guid SemesterId { get; set; }
        public DateTime? MoveInDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class AssignHouseCommandValidator : AbstractValidator<AssignHouseCommand>
    {
        public AssignHouseCommandValidator()
        {
            RuleFor(x => x.HouseId).NotEmpty();
            RuleFor(x => x.SemesterId).NotEmpty();
            RuleFor(x => x).Must(x =>
                (x.OccupantType == OccupantType.Student && x.StudentId.HasValue) ||
                (x.OccupantType == OccupantType.Lecturer && x.LecturerId.HasValue))
                .WithMessage("Either StudentId or LecturerId must be provided based on OccupantType");
        }
    }

    public class AssignHouseHandler : IRequestHandler<AssignHouseCommand, Guid>
    {
        private readonly IAccommodationRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssignHouseHandler> _logger;

        public AssignHouseHandler(
            IAccommodationRepository repository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<AssignHouseHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Guid> Handle(AssignHouseCommand request, CancellationToken cancellationToken)
        {
            // Check if house exists and is available
            var house = await _repository.GetHouseByIdAsync(request.HouseId, cancellationToken);
            if (house == null)
                throw new SMS.Application.Exceptions.NotFoundException("House", request.HouseId);

            if (house.IsOccupied)
                throw new SMS.Application.Exceptions.ValidationException($"House {house.HouseNumber} is already occupied");

            if (!house.IsAvailable || !house.IsEnabled)
                throw new SMS.Application.Exceptions.ValidationException($"House {house.HouseNumber} is not available for assignment");

            // Check if occupant already has an active assignment
            var existingAssignment = await _repository.GetAssignmentByOccupantAsync(
                request.OccupantType == OccupantType.Student ? request.StudentId!.Value : request.LecturerId!.Value,
                request.OccupantType,
                cancellationToken);
            if (existingAssignment != null && existingAssignment.Status == "Active")
                throw new SMS.Application.Exceptions.ValidationException("Occupant already has an active accommodation assignment");

            // Create assignment
            var assignment = new AccommodationAssignment
            {
                StudentId = request.OccupantType == OccupantType.Student ? request.StudentId : null,
                LecturerId = request.OccupantType == OccupantType.Lecturer ? request.LecturerId : null,
                OccupantType = request.OccupantType,
                HouseId = request.HouseId,
                LaneId = house.LaneId,
                SemesterId = request.SemesterId,
                AssignedDate = DateTime.UtcNow,
                AssignmentDate = DateTime.UtcNow,
                MoveInDate = request.MoveInDate ?? DateTime.UtcNow,
                Status = "Active",
                Remarks = request.Remarks
            };

            await _repository.AddAssignmentAsync(assignment, cancellationToken);

            // Update house status
            house.IsOccupied = true;
            house.OccupantId = request.OccupantType == OccupantType.Student ? request.StudentId : request.LecturerId;
            house.OccupantType = request.OccupantType;
            house.Status = HouseStatus.Occupied;
            house.OccupiedDate = DateTime.UtcNow;
            house.SemesterId = request.SemesterId;
            await _repository.UpdateHouseAsync(house, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var occupantId = request.OccupantType == OccupantType.Student ? request.StudentId!.Value : request.LecturerId!.Value;
            await _auditService.LogAsync("Assign", "House",
                $"Assigned {request.OccupantType} {occupantId} to house {house.HouseNumber} (LaneId: {house.LaneId})");

            _logger.LogInformation("House {HouseNumber} assigned to {OccupantType} {OccupantId}", house.HouseNumber, request.OccupantType, occupantId);
            return assignment.Id;
        }
    }
}
