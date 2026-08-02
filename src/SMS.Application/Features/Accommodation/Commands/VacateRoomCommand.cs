using FluentValidation;
using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;

namespace SMS.Application.Features.Accommodation.Commands
{
    public class VacateRoomCommand : IRequest
    {
        public Guid AssignmentId { get; set; }
        public string? Remarks { get; set; }
    }

    public class VacateRoomCommandValidator : AbstractValidator<VacateRoomCommand>
    {
        public VacateRoomCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("Assignment ID is required");
        }
    }

    public class VacateRoomCommandHandler : IRequestHandler<VacateRoomCommand>
    {
        private readonly IAccommodationRepository _accommodationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<VacateRoomCommandHandler> _logger;

        public VacateRoomCommandHandler(
            IAccommodationRepository accommodationRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<VacateRoomCommandHandler> logger)
        {
            _accommodationRepository = accommodationRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(VacateRoomCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _accommodationRepository.GetAssignmentWithDetailsAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
            {
                throw new NotFoundException("Accommodation Assignment", request.AssignmentId);
            }

            if (assignment.Status != "Active")
            {
                throw new BusinessRuleException("Cannot vacate", "Assignment is not active");
            }

            var room = assignment.Room;
            room.IsAvailable = true;

            assignment.Status = "Completed";
            assignment.MoveOutDate = DateTime.UtcNow;
            assignment.Remarks = request.Remarks ?? "Room vacated";

            await _accommodationRepository.UpdateAssignmentAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Vacate", "AccommodationAssignment", assignment.Id.ToString());

            _logger.LogInformation("Room {RoomNumber} vacated by student {StudentNumber}",
                room.RoomNumber, assignment.Student?.StudentNumber);
        }
    }
}

