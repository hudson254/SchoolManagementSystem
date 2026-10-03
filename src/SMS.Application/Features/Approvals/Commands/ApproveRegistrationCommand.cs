using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Approvals.Commands
{
    /// <summary>
    /// Approves a single student or lecturer registration.
    /// Sets RegistrationStatus to Approved and activates enrollments/allocations.
    /// </summary>
    public class ApproveRegistrationCommand : IRequest<ApprovalResultDto>
    {
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty; // "Student" or "Lecturer"
        public string? Notes { get; set; }
    }

    public class ApproveRegistrationCommandValidator : AbstractValidator<ApproveRegistrationCommand>
    {
        public ApproveRegistrationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.UserType)
                .NotEmpty().WithMessage("User type is required")
                .Must(x => x == "Student" || x == "Lecturer")
                .WithMessage("User type must be 'Student' or 'Lecturer'");
        }
    }

    public class ApprovalResultDto
    {
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ApproveRegistrationCommandHandler
        : IRequestHandler<ApproveRegistrationCommand, ApprovalResultDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitAllocationRepository _unitAllocationRepository;
        private readonly ICourseOfferingLecturerRepository _courseOfferingLecturers;
        private readonly ICourseOfferingUnitRepository _courseOfferingUnits;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBusinessEventNotifier _notifier;
        private readonly ILogger<ApproveRegistrationCommandHandler> _logger;

        public ApproveRegistrationCommandHandler(
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            IEnrollmentRepository enrollmentRepository,
            IUnitAllocationRepository unitAllocationRepository,
            ICourseOfferingLecturerRepository courseOfferingLecturers,
            ICourseOfferingUnitRepository courseOfferingUnits,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            IBusinessEventNotifier notifier,
            ILogger<ApproveRegistrationCommandHandler> logger)
        {
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _enrollmentRepository = enrollmentRepository;
            _unitAllocationRepository = unitAllocationRepository;
            _courseOfferingLecturers = courseOfferingLecturers;
            _courseOfferingUnits = courseOfferingUnits;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<ApprovalResultDto> Handle(
            ApproveRegistrationCommand request,
            CancellationToken cancellationToken)
        {
            if (request.UserType == "Student")
            {
                var student = await _studentRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (student == null)
                    throw new NotFoundException("Student", request.UserId);

                if (student.RegistrationStatus != RegistrationStatus.PendingApproval)
                    throw new SMS.Application.Exceptions.ValidationException(
                        $"Cannot approve student. Current status: {student.RegistrationStatus}. Expected: PendingApproval");

                // Update student status
                student.RegistrationStatus = RegistrationStatus.Approved;
                student.IsEnrolled = true;

                // Activate all pending enrollments
                var enrollments = await _enrollmentRepository.GetStudentEnrollmentsAsync(student.Id, cancellationToken);
                foreach (var enrollment in enrollments)
                {
                    if (enrollment.Status == "PendingApproval")
                    {
                        enrollment.Status = "Active";
                        enrollment.IsActive = true;
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync("ApproveRegistration", student.Id.ToString(),
                    $"Student registration approved. Notes: {request.Notes ?? "N/A"}");

                _logger.LogInformation("Student {StudentId} registration approved", student.Id);

                // AFTER the commit. This replaces the removed, never-registered
                // RegistrationNotificationService: the same events now flow through
                // INotificationDispatcher, so they get type/priority normalisation,
                // action-URL sanitisation and fault isolation like every other
                // notification. The recipient is the applicant's own account.
                await _notifier.NotifyRegistrationDecisionAsync(
                    student.UserId, "student", approved: true,
                    reason: request.Notes, cancellationToken);

                return new ApprovalResultDto
                {
                    UserId = student.Id,
                    UserType = "Student",
                    Status = "Approved",
                    Message = $"Student {student.FirstName} {student.LastName} registration approved successfully."
                };
            }
            else if (request.UserType == "Lecturer")
            {
                var lecturer = await _lecturerRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (lecturer == null)
                    throw new NotFoundException("Lecturer", request.UserId);

                if (lecturer.RegistrationStatus != RegistrationStatus.PendingApproval)
                    throw new SMS.Application.Exceptions.ValidationException(
                        $"Cannot approve lecturer. Current status: {lecturer.RegistrationStatus}. Expected: PendingApproval");

                // Update lecturer status
                lecturer.RegistrationStatus = RegistrationStatus.Approved;

                // Activate all pending unit allocations
                var allocations = (await _unitAllocationRepository.GetByLecturerAsync(lecturer.Id)).ToList();
                foreach (var allocation in allocations)
                {
                    if (allocation.Status == "PendingApproval")
                    {
                        allocation.Status = "Active";
                    }
                }

                // Activate the course-offering teaching assignment.
                //
                // Registration creates the `course_offering_lecturers` row as
                // "PendingConfirmation" (the lecturer has not accepted it yet), but
                // GetActiveByLecturerAsync - the relationship the lecturer dashboard,
                // and the taught-unit entitlement, are derived from - filters
                // Status == "Active". Approval is the business event that turns the
                // appointment into a real teaching load, so it must flip that row too;
                // otherwise an approved lecturer keeps an empty dashboard and is still
                // refused every privileged teaching action.
                //
                // Only THIS lecturer's own assignments are touched, and only those
                // that are still awaiting confirmation: assignments already Active,
                // Completed or Cancelled are left exactly as they are.
                var teachingAssignments = (await _courseOfferingLecturers
                        .GetByLecturerIdAsync(lecturer.Id, cancellationToken))
                    .ToList();

                var activatedAssignments = 0;
                foreach (var assignment in teachingAssignments)
                {
                    if (assignment.Status == "PendingConfirmation")
                    {
                        assignment.Status = "Active";
                        assignment.IsActive = true;
                        activatedAssignments++;
                    }
                }

                // Make sure every APPROVED unit has a valid, active offering-unit
                // relationship for its offering. Only units the lecturer actually
                // selected are considered - approval never widens an appointment to
                // the rest of the course, and never touches another lecturer's units.
                foreach (var allocation in allocations)
                {
                    if (allocation.CourseOfferingId is not Guid offeringId || offeringId == Guid.Empty)
                    {
                        continue;
                    }

                    var existing = await _courseOfferingUnits
                        .GetByOfferingAndUnitAsync(offeringId, allocation.UnitId, cancellationToken);

                    if (existing == null)
                    {
                        var nextOrder = await _courseOfferingUnits.GetMaxOrderAsync(offeringId, cancellationToken);
                        await _courseOfferingUnits.AddAsync(new CourseOfferingUnit
                        {
                            CourseOfferingId = offeringId,
                            UnitId = allocation.UnitId,
                            Order = nextOrder + 1,
                            IsActive = true
                        }, cancellationToken);
                    }
                    else if (!existing.IsActive || existing.IsDeleted)
                    {
                        existing.IsActive = true;
                        await _courseOfferingUnits.UpdateAsync(existing, cancellationToken);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _auditService.LogAsync("ApproveRegistration", lecturer.Id.ToString(),
                    $"Lecturer registration approved. {activatedAssignments} teaching assignment(s) activated " +
                    $"across {allocations.Count(a => a.CourseOfferingId.HasValue)} offering-linked allocation(s). " +
                    $"Notes: {request.Notes ?? "N/A"}");

                _logger.LogInformation(
                    "Lecturer {LecturerId} registration approved: {AllocationCount} allocation(s) active, " +
                    "{ActivatedAssignments} teaching assignment(s) activated",
                    lecturer.Id, allocations.Count, activatedAssignments);

                // AFTER the commit - see the student branch above.
                await _notifier.NotifyRegistrationDecisionAsync(
                    lecturer.UserId, "lecturer", approved: true,
                    reason: request.Notes, cancellationToken);

                return new ApprovalResultDto
                {
                    UserId = lecturer.Id,
                    UserType = "Lecturer",
                    Status = "Approved",
                    Message = $"Lecturer {lecturer.FirstName} {lecturer.LastName} registration approved successfully."
                };
            }

            throw new SMS.Application.Exceptions.ValidationException("Invalid user type");
        }
    }
}
