using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Services;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Domain.Notifications;
using Xunit;

namespace SMS.UnitTests.Notifications
{
    /// <summary>
    /// Recipient-resolution tests for <see cref="BusinessEventNotifier"/>.
    /// <para>
    /// This is the layer that decides WHO a business event reaches. The load-bearing
    /// properties are:
    /// </para>
    /// <list type="bullet">
    /// <item>a student or lecturer with no linked identity account produces NO
    /// notification (never an orphan row);</item>
    /// <item>a student enrolled in a SIBLING unit of the same course is NOT notified
    /// about a different unit's assignment;</item>
    /// <item>a dropped enrollment is not a recipient;</item>
    /// <item>the same user reached twice is passed to the dispatcher once, and the
    /// dispatcher de-duplicates.</item>
    /// <item>a repository fault is swallowed, never thrown at the business handler.</item>
    /// </list>
    /// </summary>
    public class BusinessEventNotifierTests
    {
        private readonly Mock<INotificationDispatcher> _dispatcher = new();
        private readonly Mock<IStudentRepository> _students = new();
        private readonly Mock<ILecturerRepository> _lecturers = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<IAssignmentRepository> _assignments = new();

        private BusinessEventNotifier Build() => new(
            _dispatcher.Object,
            _students.Object,
            _lecturers.Object,
            _enrollments.Object,
            _assignments.Object,
            NullLogger<BusinessEventNotifier>.Instance);

        private void StudentWithAccount(Guid studentId, string userId)
        {
            _students.Setup(s => s.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Student { Id = studentId, UserId = userId });
        }

        // ── Accommodation / registration fan-out ─────────────────────────────

        [Fact]
        public async Task NewAccount_NotifiesAdministratorCoordinatorAndReceptionist()
        {
            await Build().NotifyAccommodationRequiredAsync(
                "John Doe", "Student", "STU20260101123", "Computer Science");

            // Recipients are resolved by ROLE through the existing dispatcher, which
            // stamps the tenant and de-duplicates. This is the ONLY write path.
            _dispatcher.Verify(d => d.NotifyRolesAsync(
                It.Is<IEnumerable<string>>(roles =>
                    roles.Contains("Administrator")
                    && roles.Contains("Coordinator")
                    && roles.Contains("Receptionist")),
                "Accommodation Allocation Required",
                It.Is<string>(m =>
                    m.Contains("John Doe")            // who needs accommodation
                    && m.Contains("Student")          // their role
                    && m.Contains("STU20260101123")    // their registration id
                    && m.Contains("Computer Science") // course chosen at registration
                    && m.Contains("Accommodation allocation required")), // the action
                NotificationTypes.Accommodation,
                "STU20260101123",
                "/accommodation",
                NotificationPriorities.Important,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NewAccount_AccommodationNotification_ExcludesRolesThatCannotAllocate()
        {
            await Build().NotifyAccommodationRequiredAsync(
                "Jane Smith", "Lecturer", "LEC20260101123", "Computer Science");

            // Lecturer/Student must never receive an allocation task they cannot
            // action. Only the existing accommodation back-office tier is targeted.
            _dispatcher.Verify(d => d.NotifyRolesAsync(
                It.Is<IEnumerable<string>>(roles =>
                    !roles.Contains("Lecturer") && !roles.Contains("Student")),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NewAccount_PendingApproval_NotifiesApproversAndLinksToApprovals()
        {
            await Build().NotifyRegistrationAwaitingApprovalAsync(
                "John Doe", "Student", "Computer Science", "CS101, CS102");

            _dispatcher.Verify(d => d.NotifyRolesAsync(
                It.Is<IEnumerable<string>>(roles =>
                    roles.Contains("Administrator") && roles.Contains("Coordinator")),
                "Registration Awaiting Approval",
                It.Is<string>(m =>
                    m.Contains("John Doe")
                    && m.Contains("Computer Science")
                    && m.Contains("CS101, CS102")
                    && m.Contains("Registration awaiting approval")),
                NotificationTypes.AccountApproval,
                It.IsAny<string?>(),
                "/users",
                NotificationPriorities.Important,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AccommodationNotification_WithoutAPersonName_IsANoOp()
        {
            await Build().NotifyAccommodationRequiredAsync("   ", "Student", "STU1", "CS");

            // A blank name carries no actionable information; better to write nothing
            // than to fan out an unusable notification to every back-office user.
            _dispatcher.Verify(d => d.NotifyRolesAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Accommodation ────────────────────────────────────────────────────

        [Fact]
        public async Task AccommodationAllocated_ResolvesStudentAccount()
        {
            var studentId = Guid.NewGuid();
            StudentWithAccount(studentId, "student-user");

            await Build().NotifyAccommodationAllocatedAsync(
                studentId, null, "House A allocated", Guid.NewGuid());

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "student-user",
                It.IsAny<string>(),
                It.Is<string>(m => m.Contains("House A")),
                NotificationTypes.Accommodation,
                It.IsAny<string?>(),
                "/accommodation",
                NotificationPriorities.Important,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AccommodationAllocated_ResolvesLecturerWhenOccupantIsStaff()
        {
            var lecturerId = Guid.NewGuid();
            _lecturers.Setup(l => l.GetByIdAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Lecturer { Id = lecturerId, UserId = "lecturer-user" });

            await Build().NotifyAccommodationAllocatedAsync(null, lecturerId, "House B allocated");

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "lecturer-user", It.IsAny<string>(), It.IsAny<string>(),
                NotificationTypes.Accommodation, It.IsAny<string?>(),
                "/accommodation", NotificationPriorities.Important,
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AccommodationAllocated_SendsNothingWhenOccupantHasNoAccount()
        {
            var studentId = Guid.NewGuid();
            _students.Setup(s => s.GetByIdAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Student { Id = studentId, UserId = null });

            await Build().NotifyAccommodationAllocatedAsync(studentId, null, "House C");

            // An orphan notification addressed to a blank user id must never be written.
            _dispatcher.Verify(
                d => d.NotifyUserAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── Assignment fan-out / scoping ─────────────────────────────────────

        [Fact]
        public async Task AssignmentPublished_NotifiesOnlyStudentsEnrolledInThatUnit()
        {
            var unitId = Guid.NewGuid();
            var inUnit = Guid.NewGuid();
            var siblingUnit = Guid.NewGuid();

            // The repository's unit lookup joins through the COURSE, so it also
            // returns enrollments belonging to a different unit of the same course.
            // The notifier must filter those out, or one unit's assessment leaks.
            _enrollments.Setup(e => e.GetEnrollmentsByUnitAsync(unitId))
                .ReturnsAsync(new[]
                {
                    new Enrollment { StudentId = inUnit, UnitId = unitId, Status = "Enrolled" },
                    new Enrollment { StudentId = siblingUnit, UnitId = siblingUnit, Status = "Enrolled" },
                });
            StudentWithAccount(inUnit, "enrolled-user");
            StudentWithAccount(siblingUnit, "sibling-user");

            await Build().NotifyAssignmentPublishedAsync(
                Guid.NewGuid(), unitId, "New Assignment", "Due soon",
                includeLecturer: false);

            _dispatcher.Verify(d => d.NotifyUsersAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "enrolled-user" })),
                It.IsAny<string>(), It.IsAny<string>(),
                NotificationTypes.Assignment, It.IsAny<string?>(), "/assignments",
                NotificationPriorities.Important, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _dispatcher.Verify(d => d.NotifyUsersAsync(
                It.Is<IEnumerable<string>>(ids => ids.Contains("sibling-user")),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AssignmentPublished_ExcludesDroppedEnrollments()
        {
            var unitId = Guid.NewGuid();
            var active = Guid.NewGuid();
            var dropped = Guid.NewGuid();

            _enrollments.Setup(e => e.GetEnrollmentsByUnitAsync(unitId))
                .ReturnsAsync(new[]
                {
                    new Enrollment { StudentId = active, UnitId = unitId, Status = "Enrolled" },
                    new Enrollment { StudentId = dropped, UnitId = unitId, Status = "Dropped" },
                });
            StudentWithAccount(active, "active-user");
            StudentWithAccount(dropped, "dropped-user");

            await Build().NotifyAssignmentPublishedAsync(
                Guid.NewGuid(), unitId, "New Assignment", "Due", includeLecturer: false);

            _dispatcher.Verify(d => d.NotifyUsersAsync(
                It.Is<IEnumerable<string>>(ids => ids.Contains("dropped-user")),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AssignmentPublished_NotifiesAssigningLecturerSeparately()
        {
            var unitId = Guid.NewGuid();
            var lecturerId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();

            // No enrolled students: only the lecturer should be reached.
            _enrollments.Setup(e => e.GetEnrollmentsByUnitAsync(unitId))
                .ReturnsAsync(Array.Empty<Enrollment>());
            _assignments.Setup(a => a.GetAssignmentWithDetailsAsync(assignmentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Assignment { Id = assignmentId, LecturerId = lecturerId });
            _lecturers.Setup(l => l.GetByIdAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Lecturer { Id = lecturerId, UserId = "lecturer-user" });

            await Build().NotifyAssignmentPublishedAsync(
                assignmentId, unitId, "New Assignment", "Due", includeLecturer: true);

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "lecturer-user", It.IsAny<string>(), It.IsAny<string>(),
                NotificationTypes.Assignment, It.IsAny<string?>(), "/assignments",
                NotificationPriorities.Normal, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AssignmentSubmitted_NotifiesLecturerNotTheSubmittingStudent()
        {
            var lecturerId = Guid.NewGuid();
            _lecturers.Setup(l => l.GetByIdAsync(lecturerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Lecturer { Id = lecturerId, UserId = "lecturer-user" });

            await Build().NotifyAssignmentSubmittedAsync(
                Guid.NewGuid(), lecturerId, "Essay 1", "Jane Doe");

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "lecturer-user", It.IsAny<string>(),
                It.Is<string>(m => m.Contains("Jane Doe")),
                NotificationTypes.AssignmentSubmission, It.IsAny<string?>(),
                "/assignments", NotificationPriorities.Normal, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AssignmentGraded_NotifiesTheStudentOfTheirOwnSubmission()
        {
            var studentId = Guid.NewGuid();
            StudentWithAccount(studentId, "student-user");

            await Build().NotifyAssignmentGradedAsync(
                Guid.NewGuid(), studentId, "Essay 1", 85, 100);

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "student-user", It.IsAny<string>(),
                It.Is<string>(m => m.Contains("85/100")),
                NotificationTypes.Grade, It.IsAny<string?>(), "/assignments",
                NotificationPriorities.Important, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Units / courses ──────────────────────────────────────────────────

        [Fact]
        public async Task UnitChanged_NotifiesEnrolledStudentsOfThatUnit()
        {
            var unitId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _enrollments.Setup(e => e.GetEnrollmentsByUnitAsync(unitId))
                .ReturnsAsync(new[] { new Enrollment { StudentId = studentId, UnitId = unitId, Status = "Enrolled" } });
            StudentWithAccount(studentId, "student-user");

            await Build().NotifyUnitChangedAsync(unitId, "CSC201", "Data Structures", "renamed");

            _dispatcher.Verify(d => d.NotifyUsersAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "student-user" })),
                It.Is<string>(t => t.Contains("CSC201")), It.IsAny<string>(),
                NotificationTypes.Unit, It.IsAny<string?>(), "/units",
                NotificationPriorities.Important, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CourseChanged_SendsNothingWhenNobodyIsEnrolled()
        {
            _enrollments.Setup(e => e.GetEnrollmentsByCourseAsync(It.IsAny<Guid>()))
                .ReturnsAsync(Array.Empty<Enrollment>());

            await Build().NotifyCourseChangedAsync(
                Guid.NewGuid(), "BSC1", "Computer Science", "closed");

            _dispatcher.Verify(
                d => d.NotifyUsersAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── Enrolment / registration ────────────────────────────────────────

        [Fact]
        public async Task StudentEnrolled_NotifiesTheStudent()
        {
            var studentId = Guid.NewGuid();
            StudentWithAccount(studentId, "student-user");

            await Build().NotifyStudentEnrolledAsync(studentId, "CSC201", "Data Structures", "Sem 1");

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "student-user", It.IsAny<string>(),
                It.Is<string>(m => m.Contains("CSC201")),
                NotificationTypes.Enrollment, It.IsAny<string?>(), "/courses",
                NotificationPriorities.Important, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegistrationApproved_NotifiesTheApplicant()
        {
            await Build().NotifyRegistrationDecisionAsync("applicant-user", "student", approved: true);

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "applicant-user", It.IsAny<string>(), It.IsAny<string>(),
                NotificationTypes.Registration, "applicant-user", null,
                NotificationPriorities.Important, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegistrationRejected_CarriesTheReason()
        {
            await Build().NotifyRegistrationDecisionAsync(
                "applicant-user", "student", approved: false, reason: "Incomplete documents");

            _dispatcher.Verify(d => d.NotifyUserAsync(
                "applicant-user", It.IsAny<string>(),
                It.Is<string>(m => m.Contains("Incomplete documents")),
                NotificationTypes.Registration, "applicant-user", null,
                NotificationPriorities.Important, It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegistrationDecision_SendsNothingWithoutAnAccountId()
        {
            await Build().NotifyRegistrationDecisionAsync(null, "student", approved: true);
            await Build().NotifyRegistrationDecisionAsync("   ", "student", approved: true);

            _dispatcher.Verify(
                d => d.NotifyUserAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── Fault isolation ─────────────────────────────────────────────────

        [Fact]
        public async Task RepositoryFault_IsSwallowedSoTheBusinessHandlerStillSucceeds()
        {
            // The handler has ALREADY committed its transaction by the time it calls
            // the notifier, so a lookup failure must never propagate.
            _enrollments.Setup(e => e.GetEnrollmentsByUnitAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new InvalidOperationException("enrollment store offline"));

            var act = async () => await Build().NotifyUnitChangedAsync(
                Guid.NewGuid(), "CSC201", "Data Structures", "renamed");

            await act.Should().NotThrowAsync();
        }
    }
}