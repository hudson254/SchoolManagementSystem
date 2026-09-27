using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands
{
    /// <summary>
    /// Submits a student's course enrollment request after initial registration.
    /// Creates Enrollment records for all units in the selected course and sets
    /// status to PendingApproval.
    /// </summary>
    public class SubmitStudentEnrollmentCommand : IRequest<EnrollmentSubmissionResultDto>
    {
        public Guid CourseId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class SubmitStudentEnrollmentCommandValidator : AbstractValidator<SubmitStudentEnrollmentCommand>
    {
        public SubmitStudentEnrollmentCommandValidator()
        {
            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course selection is required");
        }
    }

    public class EnrollmentSubmissionResultDto
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int UnitsEnrolled { get; set; }
        public string Status { get; set; } = "PendingApproval";
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The course offering the pending course-offering enrollment was attached
        /// to, when an active offering exists for the selected course. Null when
        /// no active offering exists - the selection is then only recorded on the
        /// student record and the course must be scheduled by staff.
        /// </summary>
        public Guid? CourseOfferingId { get; set; }

        /// <summary>
        /// Whether a pending course-offering enrollment row was created. False
        /// when no active offering exists for the course/semester, in which case
        /// no offering is fabricated.
        /// </summary>
        public bool CourseOfferingEnrollmentCreated { get; set; }
    }

    public class SubmitStudentEnrollmentCommandHandler
        : IRequestHandler<SubmitStudentEnrollmentCommand, EnrollmentSubmissionResultDto>
    {
        private readonly SMS.Application.Common.Interfaces.ICurrentUserService _currentUserService;
        private readonly IStudentRepository _studentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseOfferingRepository _courseOfferings;
        private readonly ICourseOfferingEnrollmentRepository _courseOfferingEnrollments;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubmitStudentEnrollmentCommandHandler> _logger;

        public SubmitStudentEnrollmentCommandHandler(
            SMS.Application.Common.Interfaces.ICurrentUserService currentUserService,
            IStudentRepository studentRepository,
            ICourseRepository courseRepository,
            IUnitRepository unitRepository,
            IEnrollmentRepository enrollmentRepository,
            ICourseOfferingRepository courseOfferings,
            ICourseOfferingEnrollmentRepository courseOfferingEnrollments,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            ILogger<SubmitStudentEnrollmentCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _studentRepository = studentRepository;
            _courseRepository = courseRepository;
            _unitRepository = unitRepository;
            _enrollmentRepository = enrollmentRepository;
            _courseOfferings = courseOfferings;
            _courseOfferingEnrollments = courseOfferingEnrollments;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EnrollmentSubmissionResultDto> Handle(
            SubmitStudentEnrollmentCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated");

            // Find the student record linked to this user via email
            var userEmail = _currentUserService.Email;
            if (string.IsNullOrEmpty(userEmail))
                throw new UnauthorizedAccessException("User email not found");

            var student = await _studentRepository.GetStudentByEmailAsync(userEmail);
            if (student == null)
                throw new NotFoundException("Student record not found for current user");

            // Verify student is in PendingCourseSelection status
            if (student.RegistrationStatus != RegistrationStatus.PendingCourseSelection)
                throw new SMS.Application.Exceptions.ValidationException(
                    $"Cannot submit enrollment. Current status: {student.RegistrationStatus}. " +
                    "Expected: PendingCourseSelection");

            // Validate the course
            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null || !course.IsActive)
                throw new NotFoundException("Course", request.CourseId);

            // Get active units for the course
            var units = await _unitRepository.GetUnitsByCourseIdAsync(course.Id, cancellationToken);
            var activeUnits = units.Where(u => u.IsActive).ToList();

            if (!activeUnits.Any())
                throw new SMS.Application.Exceptions.ValidationException(
                    "No active units found for the selected course");

            // Create enrollment records for each unit
            foreach (var unit in activeUnits)
            {
                var enrollment = new Enrollment
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    UnitId = unit.Id,
                    SemesterId = request.SemesterId ?? course.SemesterId,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "PendingApproval",  // Not active until approved
                    IsActive = false              // Not active until approved
                };
                // Persist through the repository, NOT student.Enrollments.Add(...).
                //
                // BaseEntity pre-assigns Id = Guid.NewGuid(), so attaching a brand
                // new Enrollment through the navigation collection makes the EF
                // change tracker classify it as Modified instead of Added. EF then
                // issues an UPDATE for a row that does not exist, which affects
                // 0 rows and raises DbUpdateConcurrencyException (HTTP 500).
                // The repository Add path marks the entity Added and INSERTs it.
                await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            }

            // Record the choice on the student record. This is the durable,
            // single source of truth for "the course this student selected" and
            // is what the enrollment-status endpoint and the student dashboard
            // read, both at registration and here.
            student.SelectedCourseId = course.Id;

            // Update student status to PendingApproval
            student.RegistrationStatus = RegistrationStatus.PendingApproval;
            student.IsEnrolled = false; // Still not fully enrolled until approved

            // ─────────────────────────────────────────────────────────────────
            // Reconcile the two enrollment models.
            //
            // This command historically created only unit-level Enrollment rows,
            // while the student dashboard reads only course_offering_enrollments,
            // so a student who completed the wizard still saw an empty
            // dashboard. When an active CourseOffering exists for the selected
            // course we now also create a PENDING course-offering enrollment so
            // both models agree.
            //
            // The pending row uses Status = "PendingConfirmation" (not "Active"),
            // which keeps it out of GetActiveByStudentAsync. Approved,
            // offering-backed cards and grade-entry eligibility are therefore
            // completely unchanged.
            //
            // If no active offering exists we deliberately do NOT fabricate one:
            // the dashboard's PendingCourse card (fed by SelectedCourseId) shows
            // the selection instead, and staff must schedule the offering.
            // ─────────────────────────────────────────────────────────────────
            Guid? matchedOfferingId = null;
            var offerings = await _courseOfferings.GetByCourseIdAsync(course.Id, cancellationToken);
            var targetSemesterId = request.SemesterId ?? course.SemesterId;
            var candidates = offerings.Where(o => !o.IsDeleted && o.IsActive).ToList();

            var offering = (targetSemesterId.HasValue
                    ? candidates.FirstOrDefault(o => o.SemesterId == targetSemesterId)
                    : null)
                ?? (candidates.Count == 1 ? candidates[0] : null);

            if (offering == null)
            {
                _logger.LogWarning(
                    "No active course offering found for course {CourseId} ({CourseCode}). " +
                    "Enrollment recorded on the student record only; staff must schedule an offering. " +
                    "Active offering candidates: {CandidateCount}",
                    course.Id, course.Code, candidates.Count);
            }
            else if (await _courseOfferingEnrollments.ExistsByOfferingAndStudentAsync(
                         offering.Id, student.Id, cancellationToken))
            {
                // Already has a row for this offering (retry after a partial
                // failure) - reuse it rather than creating a duplicate attempt.
                matchedOfferingId = offering.Id;
                _logger.LogInformation(
                    "Course offering enrollment already exists for student {StudentId} and offering {OfferingId}",
                    student.Id, offering.Id);
            }
            else
            {
                var offeringEnrollment = new CourseOfferingEnrollment
                {
                    StudentId = student.Id,
                    CourseOfferingId = offering.Id,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "PendingConfirmation",
                    ConfirmationStatus = ConfirmationStatus.Pending,
                    IsActive = true,
                    AttemptNumber = 1,
                    Notes = "Created automatically when the student submitted course selection."
                };

                await _courseOfferingEnrollments.AddAsync(offeringEnrollment, cancellationToken);
                matchedOfferingId = offering.Id;

                _logger.LogInformation(
                    "Created pending course-offering enrollment for student {StudentId} on offering {OfferingId}",
                    student.Id, offering.Id);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var offeringNote = matchedOfferingId.HasValue
                ? $" A pending enrollment was recorded against course offering {matchedOfferingId.Value}."
                : " No active course offering is currently scheduled for this course, so your selection has been recorded and is awaiting staff scheduling.";

            await _auditService.LogAsync("SubmitEnrollment", student.Id.ToString(),
                $"Student submitted enrollment for course {course.Code} ({activeUnits.Count} units).{(matchedOfferingId.HasValue ? $" Pending offering enrollment {matchedOfferingId.Value}." : " No active course offering available; selection recorded on student record only.")}");

            _logger.LogInformation(
                "Student {StudentId} submitted enrollment for course {CourseCode} with {UnitCount} units (offering enrollment created: {OfferingCreated})",
                student.Id, course.Code, activeUnits.Count, matchedOfferingId.HasValue);

            return new EnrollmentSubmissionResultDto
            {
                StudentId = student.Id,
                CourseId = course.Id,
                CourseName = course.Name,
                UnitsEnrolled = activeUnits.Count,
                Status = "PendingApproval",
                CourseOfferingId = matchedOfferingId,
                CourseOfferingEnrollmentCreated = matchedOfferingId.HasValue,
                Message = $"Enrollment submitted for {activeUnits.Count} units. Awaiting approval.{offeringNote}"
            };
        }
    }
}
