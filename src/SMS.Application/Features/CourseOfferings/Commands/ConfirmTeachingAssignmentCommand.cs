using System;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class ConfirmTeachingAssignmentCommand : IRequest<CourseOfferingLecturerDto>
    {
        public Guid AssignmentId { get; set; }
        public bool Confirm { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class ConfirmTeachingAssignmentCommandValidator : AbstractValidator<ConfirmTeachingAssignmentCommand>
    {
        public ConfirmTeachingAssignmentCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .NotEmpty().WithMessage("Assignment ID is required");
        }
    }

    public class ConfirmTeachingAssignmentCommandHandler
        : IRequestHandler<ConfirmTeachingAssignmentCommand, CourseOfferingLecturerDto>
    {
        private readonly ICourseOfferingLecturerRepository _lecturerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ConfirmTeachingAssignmentCommandHandler> _logger;

        public ConfirmTeachingAssignmentCommandHandler(
            ICourseOfferingLecturerRepository lecturerRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<ConfirmTeachingAssignmentCommandHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CourseOfferingLecturerDto> Handle(
            ConfirmTeachingAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            var assignment = await _lecturerRepository.GetByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("CourseOfferingLecturer", request.AssignmentId);

            if (request.Confirm)
            {
                assignment.ConfirmationStatus = ConfirmationStatus.Confirmed;
                assignment.Status = "Active";
                assignment.ConfirmedDate = DateTime.UtcNow;
                assignment.Notes = request.Notes ?? assignment.Notes;
            }
            else
            {
                assignment.ConfirmationStatus = ConfirmationStatus.Pending;
                assignment.Status = "PendingConfirmation";
                assignment.ConfirmedDate = null;
                assignment.Notes = request.Notes ?? assignment.Notes;
            }

            await _lecturerRepository.UpdateAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("CourseOfferingLecturer", request.Confirm ? "Confirm" : "Unconfirm",
                assignment.Id.ToString());

            _logger.LogInformation("Teaching assignment {AssignmentId} confirmed={Confirm} for offering {OfferingId}",
                assignment.Id, request.Confirm, assignment.CourseOfferingId);

            return new CourseOfferingLecturerDto
            {
                Id = assignment.Id,
                CourseOfferingId = assignment.CourseOfferingId,
                LecturerId = assignment.LecturerId,
                LecturerName = assignment.Lecturer?.User != null
                    ? $"{assignment.Lecturer.User.FirstName} {assignment.Lecturer.User.LastName}"
                    : null,
                LecturerEmail = assignment.Lecturer?.Email,
                IsPrimary = assignment.IsPrimary,
                AssignedDate = assignment.AssignmentDate,
                IsActive = assignment.IsActive
            };
        }
    }
}
