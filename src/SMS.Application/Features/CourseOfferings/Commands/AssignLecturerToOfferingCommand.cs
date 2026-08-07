using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class AssignLecturerToOfferingCommand : IRequest<CourseOfferingLecturerDto>
    {
        public Guid CourseOfferingId { get; set; }
        public Guid LecturerId { get; set; }
        public bool IsPrimary { get; set; }
        public string? Notes { get; set; }
    }

    public class AssignLecturerToOfferingCommandValidator : AbstractValidator<AssignLecturerToOfferingCommand>
    {
        public AssignLecturerToOfferingCommandValidator()
        {
            RuleFor(x => x.CourseOfferingId)
                .NotEmpty().WithMessage("Course Offering ID is required");

            RuleFor(x => x.LecturerId)
                .NotEmpty().WithMessage("Lecturer ID is required");
        }
    }

    public class AssignLecturerToOfferingCommandHandler
        : IRequestHandler<AssignLecturerToOfferingCommand, CourseOfferingLecturerDto>
    {
        private readonly ICourseOfferingLecturerRepository _lecturerRepository;
        private readonly ICourseOfferingRepository _offeringRepository;
        private readonly ILecturerRepository _lecturerRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<AssignLecturerToOfferingCommandHandler> _logger;

        public AssignLecturerToOfferingCommandHandler(
            ICourseOfferingLecturerRepository lecturerRepository,
            ICourseOfferingRepository offeringRepository,
            ILecturerRepository lecturerRepo,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<AssignLecturerToOfferingCommandHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _offeringRepository = offeringRepository;
            _lecturerRepo = lecturerRepo;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingLecturerDto> Handle(
            AssignLecturerToOfferingCommand request,
            CancellationToken cancellationToken)
        {
            var offering = await _offeringRepository.GetByIdAsync(request.CourseOfferingId, cancellationToken);
            if (offering == null)
                throw new NotFoundException("CourseOffering", request.CourseOfferingId);

            var lecturer = await _lecturerRepo.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            // Prevent duplicate active assignment for the same offering
            var exists = await _lecturerRepository.ExistsByOfferingAndLecturerAsync(
                request.CourseOfferingId, request.LecturerId, cancellationToken);
            if (exists)
                throw new ConflictException("Lecturer is already assigned to this course offering.");

            var assignment = new CourseOfferingLecturer
            {
                CourseOfferingId = request.CourseOfferingId,
                LecturerId = request.LecturerId,
                AssignmentDate = DateTime.UtcNow,
                Status = "PendingConfirmation",
                IsActive = true,
                IsPrimary = request.IsPrimary,
                ConfirmationStatus = ConfirmationStatus.Pending,
                Notes = request.Notes
            };

            await _lecturerRepository.AddAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOfferingLecturer", "AssignLecturer", assignment.Id.ToString());

            _logger.LogInformation("Lecturer {LecturerId} assigned to offering {OfferingCode}",
                request.LecturerId, offering.OfferingCode);

            return new CourseOfferingLecturerDto
            {
                Id = assignment.Id,
                CourseOfferingId = assignment.CourseOfferingId,
                LecturerId = assignment.LecturerId,
                LecturerName = lecturer.User != null
                    ? $"{lecturer.User.FirstName} {lecturer.User.LastName}"
                    : null,
                IsPrimary = assignment.IsPrimary,
                Role = assignment.Status,
                AssignedDate = assignment.AssignmentDate,
                IsActive = assignment.IsActive
            };
        }
    }
}
