using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Accommodation.Commands
{
    public class TransferRoomCommand : IRequest<AccommodationAssignmentDto>
    {
        public Guid AssignmentId { get; set; }
        public Guid NewRoomId { get; set; }
        public string? Remarks { get; set; }
    }

    public class TransferRoomCommandValidator : AbstractValidator<TransferRoomCommand>
    {
        public TransferRoomCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("Assignment ID is required");

            RuleFor(x => x.NewRoomId)
                .NotEmpty().WithMessage("New room ID is required");
        }
    }

    public class TransferRoomCommandHandler : IRequestHandler<TransferRoomCommand, AccommodationAssignmentDto>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<TransferRoomCommandHandler> _logger;

        public TransferRoomCommandHandler(
            IAccommodationRepository accommodationRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<TransferRoomCommandHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AccommodationAssignmentDto> Handle(TransferRoomCommand request, CancellationToken cancellationToken)
        {
            var currentAssignment = await _accommodationRepository.GetAssignmentWithDetailsAsync(request.AssignmentId, cancellationToken);
            if (currentAssignment == null)
            {
                throw new NotFoundException("Accommodation Assignment", request.AssignmentId);
            }

            if (currentAssignment.Status != "Active")
            {
                throw new BusinessRuleException("Cannot transfer", "Assignment is not active");
            }

            var newRoom = await _accommodationRepository.GetRoomWithDetailsAsync(request.NewRoomId, cancellationToken);
            if (newRoom == null)
            {
                throw new NotFoundException("Room", request.NewRoomId);
            }

            if (!newRoom.IsAvailable || newRoom.IsOccupied)
            {
                throw new BusinessRuleException("Cannot transfer", "New room is not available");
            }

            // Vacate current room
            var oldRoom = currentAssignment.Room;
            oldRoom.IsAvailable = true;
            oldRoom.Vacate();

            currentAssignment.Status = "Completed";
            currentAssignment.MoveOutDate = DateTime.UtcNow;

            // Create new assignment
            var newAssignment = new AccommodationAssignment
            {
                StudentId = currentAssignment.StudentId,
                RoomId = request.NewRoomId,
                SemesterId = currentAssignment.SemesterId,
                AssignmentDate = DateTime.UtcNow,
                MoveInDate = DateTime.UtcNow,
                Status = "Active",
                Remarks = request.Remarks ?? $"Transferred from room {oldRoom.RoomNumber}"
            };

            await _accommodationRepository.AddAssignmentAsync(newAssignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogActivityAsync("AccommodationAssignment", "Transfer", newAssignment.Id.ToString(), "transfer");

            _logger.LogInformation("Student transferred from room {OldRoom} to room {NewRoom}",
                oldRoom.RoomNumber, newRoom.RoomNumber);

            return new AccommodationAssignmentDto
            {
                Id = newAssignment.Id,
                StudentId = newAssignment.StudentId,
                RoomId = newAssignment.RoomId ?? Guid.Empty,
                SemesterId = newAssignment.SemesterId,
                AssignmentDate = newAssignment.AssignmentDate,
                MoveInDate = newAssignment.MoveInDate,
                MoveOutDate = newAssignment.MoveOutDate,
                Status = newAssignment.Status,
                Remarks = newAssignment.Remarks,
                StudentName = newAssignment.Student?.User.FullName ?? string.Empty,
                StudentNumber = newAssignment.Student?.StudentNumber ?? string.Empty,
                RoomNumber = newRoom.RoomNumber,
                BlockName = newRoom.Block?.Name ?? string.Empty,
                BuildingName = newRoom.Block?.Building ?? string.Empty,
                SemesterName = newAssignment.Semester?.Name ?? string.Empty
            };
        }
    }
}






