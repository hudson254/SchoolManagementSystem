using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;

namespace SMS.Application.Services
{
    /// <summary>
    /// Default <see cref="IBusinessEventNotifier"/>.
    /// <para>
    /// Two responsibilities, deliberately kept together:
    /// </para>
    /// <list type="number">
    /// <item><b>Recipient resolution.</b> Maps a business relationship (a unit, an
    /// assignment's lecturer, an occupant) onto identity user ids. This lives here,
    /// once, rather than in each handler.</item>
    /// <item><b>Delegation.</b> The actual row is written by
    /// <see cref="INotificationDispatcher"/>, which owns normalisation,
    /// de-duplication, action-URL sanitisation and fault isolation. This class never
    /// writes a notification itself, so there is still exactly one write path.</item>
    /// </list>
    /// <para>
    /// <b>Tenant isolation.</b> Every recipient lookup goes through a repository
    /// carrying the global EF tenant query filter (and, for Notifications,
    /// PostgreSQL RLS). A foreign tenant's id therefore resolves to nothing and is
    /// silently dropped rather than becoming a recipient.
    /// </para>
    /// <para>
    /// <b>Fault isolation.</b> Resolution failures are caught and logged. A handler
    /// calls this AFTER its own <c>SaveChangesAsync</c>, so the authoritative state
    /// change has already committed and must never be undone by a notification
    /// problem.
    /// </para>
    /// </summary>
    public class BusinessEventNotifier : IBusinessEventNotifier
    {
        /// <summary>
        /// Application-relative deep links. Every value is passed through
        /// <see cref="NotificationCatalog.NormalizeActionUrl"/> by the dispatcher,
        /// so a typo here degrades to "no button" rather than to an unsafe link.
        /// </summary>
        private const string AccommodationUrl = "/accommodation";
        private const string AssignmentUrl = "/assignments";
        private const string UnitUrl = "/units";
        private const string CourseUrl = "/courses";
        private const string EnrollmentUrl = "/courses";

        private readonly INotificationDispatcher _dispatcher;
        private readonly IStudentRepository _studentRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<BusinessEventNotifier> _logger;

        public BusinessEventNotifier(
            INotificationDispatcher dispatcher,
            IStudentRepository studentRepository,
            ILecturerRepository lecturerRepository,
            IEnrollmentRepository enrollmentRepository,
            IAssignmentRepository assignmentRepository,
            ILogger<BusinessEventNotifier> logger)
        {
            _dispatcher = dispatcher;
            _studentRepository = studentRepository;
            _lecturerRepository = lecturerRepository;
            _enrollmentRepository = enrollmentRepository;
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        // ── Recipient resolution ──────────────────────────────────────────────

        /// <summary>
        /// Resolves the identity user id for a student, or null when the student is
        /// absent, has no linked account, or belongs to another tenant.
        /// </summary>
        private async Task<string?> ResolveStudentUserIdAsync(Guid? studentId, CancellationToken ct)
        {
            if (!studentId.HasValue || studentId.Value == Guid.Empty) return null;
            try
            {
                var student = await _studentRepository.GetByIdAsync(studentId.Value, ct);
                return string.IsNullOrWhiteSpace(student?.UserId) ? null : student!.UserId;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to resolve user id for student {StudentId}", studentId);
                return null;
            }
        }

        /// <summary>
        /// Resolves the identity user id for a lecturer, or null when absent /
        /// unlinked / cross-tenant.
        /// </summary>
        private async Task<string?> ResolveLecturerUserIdAsync(Guid? lecturerId, CancellationToken ct)
        {
            if (!lecturerId.HasValue || lecturerId.Value == Guid.Empty) return null;
            try
            {
                var lecturer = await _lecturerRepository.GetByIdAsync(lecturerId.Value, ct);
                return string.IsNullOrWhiteSpace(lecturer?.UserId) ? null : lecturer!.UserId;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to resolve user id for lecturer {LecturerId}", lecturerId);
                return null;
            }
        }

        /// <summary>
        /// Resolves the user ids of students ACTIVELY enrolled in a specific unit.
        /// <para>
        /// <see cref="IEnrollmentRepository.GetEnrollmentsByUnitAsync"/> is NOT used
        /// on its own: it joins through the course's unit list, so it also returns
        /// students enrolled in a sibling unit of the same course. Notifying those
        /// would leak one unit's assessment to students who are not enrolled in it.
        /// This filters on the enrollment's own <c>UnitId</c> (keeping legacy rows
        /// that predate that column being populated) and excludes dropped enrollments.
        /// </para>
        /// </summary>
        private async Task<IReadOnlyCollection<string>> ResolveEnrolledStudentUserIdsAsync(
            Guid unitId, CancellationToken ct)
        {
            if (unitId == Guid.Empty) return Array.Empty<string>();

            try
            {
                var enrollments = await _enrollmentRepository.GetEnrollmentsByUnitAsync(unitId);
                if (enrollments == null) return Array.Empty<string>();

                var studentIds = enrollments
                    .Where(e => e != null && !e.IsDeleted)
                    .Where(e => e.UnitId == null || e.UnitId == unitId)
                    .Where(e => !string.Equals(e.Status, "Dropped", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.StudentId)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (studentIds.Count == 0) return Array.Empty<string>();

                var userIds = new List<string>(studentIds.Count);
                foreach (var studentId in studentIds)
                {
                    var userId = await ResolveStudentUserIdAsync(studentId, ct);
                    if (userId != null) userIds.Add(userId);
                }

                return userIds;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to resolve enrolled students for unit {UnitId}", unitId);
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Resolves the user ids of students enrolled in a course (any of its units).
        /// Used for course-level announcements.
        /// </summary>
        private async Task<IReadOnlyCollection<string>> ResolveCourseStudentUserIdsAsync(
            Guid courseId, CancellationToken ct)
        {
            if (courseId == Guid.Empty) return Array.Empty<string>();

            try
            {
                var enrollments = await _enrollmentRepository.GetEnrollmentsByCourseAsync(courseId);
                if (enrollments == null) return Array.Empty<string>();

                var studentIds = enrollments
                    .Where(e => e != null && !e.IsDeleted)
                    .Where(e => !string.Equals(e.Status, "Dropped", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.StudentId)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                var userIds = new List<string>(studentIds.Count);
                foreach (var studentId in studentIds)
                {
                    var userId = await ResolveStudentUserIdAsync(studentId, ct);
                    if (userId != null) userIds.Add(userId);
                }

                return userIds;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to resolve enrolled students for course {CourseId}", courseId);
                return Array.Empty<string>();
            }
        }

        // ── Accommodation ────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the occupant (student OR lecturer) to a single user id. An
        /// allocation is made to exactly one of the two, so a student id always wins
        /// when both are somehow supplied.
        /// </summary>
        private async Task<string?> ResolveOccupantUserIdAsync(
            Guid? studentId, Guid? lecturerId, CancellationToken ct)
        {
            var fromStudent = await ResolveStudentUserIdAsync(studentId, ct);
            return fromStudent ?? await ResolveLecturerUserIdAsync(lecturerId, ct);
        }

        public async Task NotifyAccommodationAllocatedAsync(
            Guid? studentId, Guid? lecturerId, string description, Guid? referenceId = null,
            CancellationToken cancellationToken = default)
        {
            var userId = await ResolveOccupantUserIdAsync(studentId, lecturerId, cancellationToken);
            if (userId == null) return;

            await _dispatcher.NotifyUserAsync(
                userId,
                "Accommodation Allocated",
                description,
                NotificationTypes.Accommodation,
                referenceId?.ToString(),
                AccommodationUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        public async Task NotifyAccommodationChangedAsync(
            Guid? studentId, Guid? lecturerId, string title, string message, Guid? referenceId = null,
            CancellationToken cancellationToken = default)
        {
            var userId = await ResolveOccupantUserIdAsync(studentId, lecturerId, cancellationToken);
            if (userId == null) return;

            await _dispatcher.NotifyUserAsync(
                userId, title, message,
                NotificationTypes.Accommodation,
                referenceId?.ToString(),
                AccommodationUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        public async Task NotifyAccommodationEndedAsync(
            Guid? studentId, Guid? lecturerId, string message, Guid? referenceId = null,
            CancellationToken cancellationToken = default)
        {
            var userId = await ResolveOccupantUserIdAsync(studentId, lecturerId, cancellationToken);
            if (userId == null) return;

            await _dispatcher.NotifyUserAsync(
                userId,
                "Accommodation Ended",
                message,
                NotificationTypes.Accommodation,
                referenceId?.ToString(),
                AccommodationUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        // ── Assignments ──────────────────────────────────────────────────────

        public async Task NotifyAssignmentPublishedAsync(
            Guid assignmentId, Guid unitId, string title, string message, bool includeLecturer = true,
            CancellationToken cancellationToken = default)
        {
            // Students enrolled in the unit. The dispatcher de-duplicates, so a
            // student holding several enrollments for the unit gets one copy.
            var students = await ResolveEnrolledStudentUserIdsAsync(unitId, cancellationToken);

            if (students.Count > 0)
            {
                await _dispatcher.NotifyUsersAsync(
                    students, title, message,
                    NotificationTypes.Assignment,
                    assignmentId.ToString(),
                    AssignmentUrl,
                    NotificationPriorities.Important,
                    cancellationToken: cancellationToken);
            }

            if (!includeLecturer) return;

            // The lecturer who owns the unit is told as well, at a lower priority:
            // the students are the audience for the assessment itself, the lecturer
            // needs to know it is live.
            try
            {
                var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(assignmentId, cancellationToken);
                var lecturerUserId = await ResolveLecturerUserIdAsync(assignment?.LecturerId, cancellationToken);
                if (lecturerUserId != null)
                {
                    await _dispatcher.NotifyUserAsync(
                        lecturerUserId, title, message,
                        NotificationTypes.Assignment,
                        assignmentId.ToString(),
                        AssignmentUrl,
                        NotificationPriorities.Normal,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to notify lecturer for assignment {AssignmentId}", assignmentId);
            }
        }

        public async Task NotifyAssignmentSubmittedAsync(
            Guid assignmentId, Guid lecturerId, string assignmentTitle, string studentName,
            CancellationToken cancellationToken = default)
        {
            var lecturerUserId = await ResolveLecturerUserIdAsync(lecturerId, cancellationToken);
            if (lecturerUserId == null) return;

            await _dispatcher.NotifyUserAsync(
                lecturerUserId,
                "Assignment Submitted",
                $"{studentName} submitted \"{assignmentTitle}\" for marking.",
                NotificationTypes.AssignmentSubmission,
                assignmentId.ToString(),
                AssignmentUrl,
                NotificationPriorities.Normal,
                cancellationToken: cancellationToken);
        }

        public async Task NotifyAssignmentGradedAsync(
            Guid assignmentId, Guid studentId, string assignmentTitle, int score, int maxScore,
            CancellationToken cancellationToken = default)
        {
            var studentUserId = await ResolveStudentUserIdAsync(studentId, cancellationToken);
            if (studentUserId == null) return;

            await _dispatcher.NotifyUserAsync(
                studentUserId,
                "Assignment Graded",
                $"Your submission for \"{assignmentTitle}\" has been graded: {score}/{maxScore}.",
                NotificationTypes.Grade,
                assignmentId.ToString(),
                AssignmentUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        public async Task NotifyAssignmentIssueAsync(
            Guid assignmentId, Guid studentId, string title, string message,
            CancellationToken cancellationToken = default)
        {
            var studentUserId = await ResolveStudentUserIdAsync(studentId, cancellationToken);
            if (studentUserId == null) return;

            await _dispatcher.NotifyUserAsync(
                studentUserId, title, message,
                NotificationTypes.AssignmentIssue,
                assignmentId.ToString(),
                AssignmentUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        // ── Units ────────────────────────────────────────────────────────────

        public async Task NotifyUnitChangedAsync(
            Guid unitId, string unitCode, string unitName, string message,
            CancellationToken cancellationToken = default)
        {
            var students = await ResolveEnrolledStudentUserIdsAsync(unitId, cancellationToken);
            if (students.Count == 0) return;

            await _dispatcher.NotifyUsersAsync(
                students,
                $"Unit Update: {unitCode}",
                $"{unitName}: {message}",
                NotificationTypes.Unit,
                unitId.ToString(),
                UnitUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        // ── Courses ──────────────────────────────────────────────────────────

        public async Task NotifyCourseChangedAsync(
            Guid courseId, string courseCode, string courseName, string message,
            CancellationToken cancellationToken = default)
        {
            var students = await ResolveCourseStudentUserIdsAsync(courseId, cancellationToken);
            if (students.Count == 0) return;

            await _dispatcher.NotifyUsersAsync(
                students,
                $"Course Update: {courseCode}",
                $"{courseName}: {message}",
                NotificationTypes.Course,
                courseId.ToString(),
                CourseUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        // ── Enrolment ────────────────────────────────────────────────────────

        public async Task NotifyStudentEnrolledAsync(
            Guid studentId, string unitCode, string unitName, string semesterName,
            CancellationToken cancellationToken = default)
        {
            var studentUserId = await ResolveStudentUserIdAsync(studentId, cancellationToken);
            if (studentUserId == null) return;

            var semester = string.IsNullOrWhiteSpace(semesterName)
                ? string.Empty
                : $" for {semesterName}";

            await _dispatcher.NotifyUserAsync(
                studentUserId,
                "Enrolment Confirmed",
                $"You have been enrolled in unit {unitCode} - {unitName}{semester}.",
                NotificationTypes.Enrollment,
                studentId.ToString(),
                EnrollmentUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        public async Task NotifyEnrollmentStatusChangedAsync(
            Guid studentId, string status, string unitName,
            CancellationToken cancellationToken = default)
        {
            var studentUserId = await ResolveStudentUserIdAsync(studentId, cancellationToken);
            if (studentUserId == null) return;

            await _dispatcher.NotifyUserAsync(
                studentUserId,
                "Enrolment Updated",
                $"Your enrolment in {unitName} is now \"{status}\".",
                NotificationTypes.Enrollment,
                studentId.ToString(),
                EnrollmentUrl,
                NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }

        // ── Registration / account approval ──────────────────────────────────

        /// <summary>
        /// Registration outcome. The recipient is the applicant themselves, so the
        /// user id is supplied directly by the approval handler (which already has
        /// the account it is approving) rather than resolved through the academic
        /// graph. A blank id is a no-op, so an unlinked record cannot produce an
        /// orphan row.
        /// </summary>
        public async Task NotifyRegistrationDecisionAsync(
            string? userId, string userType, bool approved, string? reason = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var subject = string.IsNullOrWhiteSpace(userType) ? "Your" : $"Your {userType}";

            if (approved)
            {
                await _dispatcher.NotifyUserAsync(
                    userId,
                    "Registration Approved",
                    $"{subject} registration has been approved. You now have full access.",
                    NotificationTypes.Registration,
                    userId,
                    actionUrl: null,
                    priority: NotificationPriorities.Important,
                    cancellationToken: cancellationToken);
                return;
            }

            var detail = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Reason: {reason.Trim()}";

            await _dispatcher.NotifyUserAsync(
                userId,
                "Registration Rejected",
                $"{subject} registration has been rejected.{detail}",
                NotificationTypes.Registration,
                userId,
                actionUrl: null,
                priority: NotificationPriorities.Important,
                cancellationToken: cancellationToken);
        }
    }
}