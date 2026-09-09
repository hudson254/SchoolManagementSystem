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
        public Guid? SemesterId { get; set; }
        public DateTime? MoveInDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class AssignHouseCommandValidator : AbstractValidator<AssignHouseCommand>
    {
        public AssignHouseCommandValidator()
        {
            RuleFor(x => x.HouseId).NotEmpty();
            RuleFor(x => x).Must(x =>
                (x.OccupantType == OccupantType.Student && x.StudentId.HasValue) ||
                (x.OccupantType == OccupantType.Lecturer && x.LecturerId.HasValue))
                .WithMessage("Either StudentId or LecturerId must be provided based on OccupantType");
        }
    }

    public class AssignHouseHandler : IRequestHandler<AssignHouseCommand, Guid>
    {
        private readonly IAccommodationRepository _repository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssignHouseHandler> _logger;

        public AssignHouseHandler(
            IAccommodationRepository repository,
            ISemesterRepository semesterRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<AssignHouseHandler> logger)
        {
            _repository = repository;
            _semesterRepository = semesterRepository;
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

            if (house.Status == HouseStatus.Maintenance ||
                house.Status == HouseStatus.Disabled ||
                house.Status == HouseStatus.Unavailable ||
                house.Status == HouseStatus.Reserved)
                throw new SMS.Application.Exceptions.ValidationException(
                    $"House {house.HouseNumber} is currently {house.Status.ToLower()} and cannot receive new occupants");

            if (!house.IsAvailable || !house.IsEnabled)
                throw new SMS.Application.Exceptions.ValidationException($"House {house.HouseNumber} is not available for assignment");

            // Capacity enforcement (multi-occupancy: students + lecturers share the same pool)
            if (house.OccupiedCount >= house.Capacity)
                throw new SMS.Application.Exceptions.ValidationException(
                    $"House {house.HouseNumber} has reached its capacity ({house.OccupiedCount}/{house.Capacity}) and cannot accept more occupants");

            var semesterId = request.SemesterId;
            if (!semesterId.HasValue)
            {
                var semester = await _semesterRepository.GetCurrentOrDefaultAsync(cancellationToken);
                if (semester == null)
                    throw new SMS.Application.Exceptions.ValidationException(
                        "A semester is required to assign accommodation and no current semester could be resolved. Please create a semester or provide SemesterId.");
                semesterId = semester.Id;
            }

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
                SemesterId = semesterId.Value,
                AssignedDate = DateTime.UtcNow,
                AssignmentDate = DateTime.UtcNow,
                MoveInDate = request.MoveInDate ?? DateTime.UtcNow,
                Status = "Active",
                Remarks = request.Remarks
            };

            await _repository.AddAssignmentAsync(assignment, cancellationToken);

            // Update house occupancy state (multi-occupancy aware)
            house.OccupiedCount += 1;
            house.IsOccupied = house.OccupiedCount > 0;
            if (!house.OccupantId.HasValue)
            {
                house.OccupantId = request.OccupantType == OccupantType.Student ? request.StudentId : request.LecturerId;
                house.OccupantType = request.OccupantType;
            }
            house.Status = HouseStatus.Occupied;
            house.OccupiedDate = DateTime.UtcNow;
            house.SemesterId = semesterId.Value;
            await _repository.UpdateHouseAsync(house, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var occupantId = request.OccupantType == OccupantType.Student ? request.StudentId!.Value : request.LecturerId!.Value;
            await _auditService.LogAsync("Assign", "House",
                $"Assigned {request.OccupantType} {occupantId} to house {house.HouseNumber} (HouseId: {house.Id}, LaneId: {house.LaneId}, Occupancy: {house.OccupiedCount}/{house.Capacity})");

            _logger.LogInformation("House {HouseNumber} assigned to {OccupantType} {OccupantId}, occupancy now {Occupied}/{Capacity}",
                house.HouseNumber, request.OccupantType, occupantId, house.OccupiedCount, house.Capacity);
            return assignment.Id;
        }
    }
}
