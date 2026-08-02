using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Accommodation.Commands
{
    public class AssignRoomCommand : IRequest<AccommodationAssignmentDto>
    {
        public Guid RoomId { get; set; }
        public Guid StudentId { get; set; }
        public Guid SemesterId { get; set; }
        public DateTime? MoveInDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class AssignRoomCommandValidator : AbstractValidator<AssignRoomCommand>
    {
        public AssignRoomCommandValidator()
        {
            RuleFor(x => x.RoomId)
                .NotEmpty().WithMessage("Room ID is required");

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required");

            RuleFor(x => x.SemesterId)
                .NotEmpty().WithMessage("Semester ID is required");
        }
    }

    public class AssignRoomCommandHandler : IRequestHandler<AssignRoomCommand, AccommodationAssignmentDto>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssignRoomCommandHandler> _logger;

        public AssignRoomCommandHandler(
            IAccommodationRepository accommodationRepository,
            IStudentRepository studentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<AssignRoomCommandHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _studentRepository = studentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AccommodationAssignmentDto> Handle(AssignRoomCommand request, CancellationToken cancellationToken)
        {
            var room = await _accommodationRepository.GetRoomWithDetailsAsync(request.RoomId, cancellationToken);
            if (room == null)
            {
                throw new NotFoundException("Room", request.RoomId);
            }

            if (!room.IsAvailable || room.IsOccupied)
            {
                throw new BusinessRuleException("Cannot assign room", "Room is not available for assignment");
            }

            var student = await _studentRepository.GetStudentWithDetailsAsync(request.StudentId, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            // Check if student already has accommodation for this semester
            var existingAssignment = await _accommodationRepository.GetAssignmentByStudentAndSemesterAsync(
                request.StudentId,
                request.SemesterId,
                cancellationToken);

            if (existingAssignment != null && existingAssignment.Status == "Active")
            {
                throw new ConflictException("Accommodation", "Student-Semester", $"{request.StudentId}-{request.SemesterId}");
            }

            var assignment = new AccommodationAssignment
            {
                StudentId = request.StudentId,
                RoomId = request.RoomId,
                SemesterId = request.SemesterId,
                AssignmentDate = DateTime.UtcNow,
                MoveInDate = request.MoveInDate ?? DateTime.UtcNow,
                Status = "Active",
                Remarks = request.Remarks
            };

            await _accommodationRepository.AddAssignmentAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("Assign", "AccommodationAssignment", assignment.Id.ToString(),
                            $"Student: {student.StudentNumber}");

            _logger.LogInformation("Room {RoomNumber} assigned to student {StudentNumber}",
                room.RoomNumber, student.StudentNumber);

            return new AccommodationAssignmentDto
            {
                Id = assignment.Id,
                StudentId = assignment.StudentId,
                RoomId = assignment.RoomId ?? Guid.Empty,
                SemesterId = assignment.SemesterId,
                AssignmentDate = assignment.AssignmentDate,
                MoveInDate = assignment.MoveInDate,
                MoveOutDate = assignment.MoveOutDate,
                Status = assignment.Status,
                Remarks = assignment.Remarks,
                StudentName = student.User.FullName,
                StudentNumber = student.StudentNumber,
                RoomNumber = room.RoomNumber,
                BlockName = room.Block?.Name ?? string.Empty,
                BuildingName = room.Block?.Building ?? string.Empty,
                SemesterName = assignment.Semester?.Name ?? string.Empty
            };
        }
    }
}




