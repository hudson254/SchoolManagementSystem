using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Auth.Commands;
using SMS.Application.Services;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.UnitTests.Common;
using Xunit;

namespace SMS.UnitTests.Auth
{
    /// <summary>
    /// Regression tests for the reported production defect: "a lecturer's course and
    /// unit selections do not appear after login", where the dashboard said
    /// "You are not assigned to teach any course offerings yet" and Study Materials
    /// said "You are not currently appointed to teach any unit".
    ///
    /// <para>
    /// Root cause: registration only wrote <c>UnitAllocation</c> rows. The lecturer
    /// dashboard reads <c>course_offering_lecturers</c> (which registration never
    /// created), and <c>LecturerRepository.GetTaughtUnitIdsAsync</c> resolves taught
    /// units from ACTIVE allocations plus offering-unit rows. So the selections were
    /// written to one table and read from another.
    ///
    /// <para>
    /// Registration also left the lecturer in <c>PendingCourseSelection</c>, which
    /// <c>ApproveRegistrationCommand</c> rejects - so a lecturer registration could
    /// never be approved by anyone.
    /// </para>
    /// </summary>
    public class LecturerRegistrationRelationshipTests
    {
        private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid SemesterId = Guid.NewGuid();

        private readonly Mock<IUserManagerService> _userManager = new();
        private readonly Mock<IJwtService> _jwt = new();
        private readonly Mock<IAuditService> _audit = new();
        private readonly Mock<IUsernameGenerator> _usernameGenerator = new();
        private readonly Mock<INameParser> _nameParser = new();
        private readonly Mock<ILecturerRepository> _lecturerRepo = new();
        private readonly Mock<ICourseRepository> _courseRepo = new();
        private readonly Mock<IUnitRepository> _unitRepo = new();
        private readonly Mock<IUnitAllocationRepository> _allocationRepo = new();
        private readonly Mock<ISemesterRepository> _semesterRepo = new();
        private readonly Mock<ICourseOfferingRepository> _offeringRepo = new();
        private readonly Mock<ICourseOfferingEnrollmentRepository> _offeringEnrollmentRepo = new();
        private readonly Mock<ICourseOfferingLecturerRepository> _offeringLecturerRepo = new();
        private readonly Mock<ICourseOfferingUnitRepository> _offeringUnitRepo = new();
        private readonly Mock<IEnrollmentRepository> _enrollmentRepo = new();
        private readonly Mock<IBusinessEventNotifier> _notifier = new();
        private readonly Mock<SMS.Multitenancy.Interfaces.ITenantContext> _tenantContext = new();
        private readonly Mock<IUnitOfWork> _unitOfWork =
            new Mock<IUnitOfWork>().RunsTransactionInline<SMS.Domain.Entities.User>();

        private RegisterCommandHandler CreateHandler() =>
            new(
                _userManager.Object, _jwt.Object, _audit.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler>>(),
                _usernameGenerator.Object, _nameParser.Object,
                new Mock<IStudentRepository>().Object, _lecturerRepo.Object,
                _courseRepo.Object, _unitRepo.Object, _allocationRepo.Object,
                _semesterRepo.Object, _tenantContext.Object, _unitOfWork.Object,
                new PasswordPolicyService(),
                _enrollmentRepo.Object, _offeringRepo.Object, _offeringEnrollmentRepo.Object,
                _offeringLecturerRepo.Object, _offeringUnitRepo.Object, _notifier.Object);

        private static RegisterCommand BuildLecturerCommand(Guid courseId, IEnumerable<Guid> unitIds) =>
            new()
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = "Test123!@#abcd",
                ConfirmPassword = "Test123!@#abcd",
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science",
                CourseId = courseId,
                UnitIds = unitIds.ToList()
            };

        private static SMS.Domain.Entities.Unit ActiveUnit(Guid courseId, string code, string name) =>
            new() { Id = Guid.NewGuid(), Code = code, Name = name, CourseId = courseId, IsActive = true };

        private void ArrangeHappyPath(RegisterCommand command)
        {
            _tenantContext.Setup(x => x.TenantId).Returns(TenantId.ToString());
            _userManager.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null!);
            _nameParser.Setup(x => x.ParseName(It.IsAny<string>())).Returns(new NameParseResult
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                IsValid = true
            });
            _usernameGenerator.Setup(x => x.GenerateUsernameAsync("Jane", "Smith")).ReturnsAsync("jane.smith");
            _userManager
                .Setup(x => x.CreateUserAsync("jane.smith", command.Email, command.Password, command.Role))
                .ReturnsAsync((string u, string e, string p, string r) => new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = e,
                    UserName = u,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    IsActive = true
                });
            _userManager.Setup(x => x.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "Lecturer" });
            _jwt.Setup(x => x.GenerateAccessToken(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");
            _userManager.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");

            // The course carries no semester of its own, so the handler resolves the
            // tenant's current academic period.
            _semesterRepo.Setup(x => x.GetCurrentOrDefaultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Semester { Id = SemesterId, Name = "Semester 1", IsActive = true });
        }

        private void ArrangeCourse(Guid courseId, string courseName, params SMS.Domain.Entities.Unit[] units)
        {
            _courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = courseName, Code = "CSC", IsActive = true });
            _unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(units.ToList());
        }

        private void ArrangeSingleActiveOffering(Guid courseId, Guid offeringId)
        {
            _offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering> { new() { Id = offeringId, CourseId = courseId, IsActive = true } });
        }

        [Fact]
        public async Task LecturerRegistration_CreatesCourseOfferingAssignment_SoDashboardIsNotEmpty()
        {
            var courseId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            var unit = ActiveUnit(courseId, "CS101", "Intro");
            var command = BuildLecturerCommand(courseId, new[] { unit.Id });
            ArrangeHappyPath(command);
            ArrangeCourse(courseId, "CS", unit);
            ArrangeSingleActiveOffering(courseId, offeringId);

            _offeringLecturerRepo
                .Setup(x => x.ExistsByOfferingAndLecturerAsync(offeringId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            Lecturer? captured = null;
            _lecturerRepo.Setup(x => x.AddAsync(It.IsAny<Lecturer>(), It.IsAny<CancellationToken>()))
                .Callback<Lecturer, CancellationToken>((l, _) => captured = l)
                .ReturnsAsync((Lecturer l, CancellationToken _) => l);

            var assignments = new List<CourseOfferingLecturer>();
            _offeringLecturerRepo.Setup(x => x.AddAsync(It.IsAny<CourseOfferingLecturer>(), It.IsAny<CancellationToken>()))
                .Callback<CourseOfferingLecturer, CancellationToken>((a, _) => assignments.Add(a))
                .ReturnsAsync((CourseOfferingLecturer a, CancellationToken _) => a);

            await CreateHandler().Handle(command, CancellationToken.None);

            // THIS is the row the lecturer dashboard reads. Without it the lecturer
            // always saw "not assigned to teach any course offerings yet".
            assignments.Should().HaveCount(1);
            assignments[0].CourseOfferingId.Should().Be(offeringId);
            assignments[0].LecturerId.Should().Be(captured!.Id);

            // Active + PendingConfirmation matches the pre-existing contract: the
            // assignment is visible immediately, while privileged teaching/material
            // write actions stay gated until an administrator approves.
            assignments[0].IsActive.Should().BeTrue();
            assignments[0].Status.Should().Be("PendingConfirmation");
            assignments[0].ConfirmationStatus.Should().Be(ConfirmationStatus.Pending);
        }

        [Fact]
        public async Task LecturerRegistration_SetsPendingApproval_SoTheApprovalWorkflowCanActOnIt()
        {
            var courseId = Guid.NewGuid();
            var unit = ActiveUnit(courseId, "CS101", "Intro");
            var command = BuildLecturerCommand(courseId, new[] { unit.Id });
            ArrangeHappyPath(command);
            ArrangeCourse(courseId, "CS", unit);
            _offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            Lecturer? captured = null;
            _lecturerRepo.Setup(x => x.AddAsync(It.IsAny<Lecturer>(), It.IsAny<CancellationToken>()))
                .Callback<Lecturer, CancellationToken>((l, _) => captured = l)
                .ReturnsAsync((Lecturer l, CancellationToken _) => l);

            await CreateHandler().Handle(command, CancellationToken.None);

            // PendingCourseSelection made ApproveRegistrationCommand throw
            // ("Expected: PendingApproval"), so no administrator could ever clear a
            // new lecturer registration.
            captured!.RegistrationStatus.Should().Be(RegistrationStatus.PendingApproval);
        }

        [Fact]
        public async Task LecturerRegistration_AllocatesOnlyTheSelectedUnits_NotEveryUnitOfTheCourse()
        {
            var courseId = Guid.NewGuid();
            var chosen = ActiveUnit(courseId, "CS101", "A");
            var notChosen = ActiveUnit(courseId, "CS102", "B");
            var command = BuildLecturerCommand(courseId, new[] { chosen.Id });
            ArrangeHappyPath(command);
            ArrangeCourse(courseId, "CS", chosen, notChosen);
            _offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            var allocations = new List<UnitAllocation>();
            _allocationRepo.Setup(x => x.AddAsync(It.IsAny<UnitAllocation>(), It.IsAny<CancellationToken>()))
                .Callback<UnitAllocation, CancellationToken>((a, _) => allocations.Add(a))
                .ReturnsAsync((UnitAllocation a, CancellationToken _) => a);

            await CreateHandler().Handle(command, CancellationToken.None);

            // "All units" and "specific units" are both supported; a lecturer who
            // chose one unit must not silently be assigned the whole course.
            allocations.Should().HaveCount(1);
            allocations[0].UnitId.Should().Be(chosen.Id);
            allocations.Should().OnlyContain(a => a.Status == "PendingApproval");
        }

        [Fact]
        public async Task LecturerRegistration_RejectsUnitFromAnotherCourse_SoCrossCourseAssignmentIsImpossible()
        {
            var courseId = Guid.NewGuid();
            var foreignUnitId = Guid.NewGuid();
            var command = BuildLecturerCommand(courseId, new[] { foreignUnitId });
            ArrangeHappyPath(command);

            // The tenant-scoped course resolves and has units, but the submitted unit
            // is NOT one of them - it belongs to another course or another tenant,
            // which the tenant query filter makes indistinguishable.
            ArrangeCourse(courseId, "CS", ActiveUnit(courseId, "CS101", "Owned"));
            _offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            var ex = await Assert.ThrowsAsync<SMS.Application.Exceptions.ValidationException>(
                () => CreateHandler().Handle(command, CancellationToken.None));

            ex.Message.Should().Contain("do not belong to the selected course");

            _lecturerRepo.Verify(
                x => x.AddAsync(It.IsAny<Lecturer>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "a tampered unit selection must never create an account");
        }

        [Fact]
        public async Task LecturerRegistration_RaisesAccommodationAndApprovalNotifications()
        {
            var courseId = Guid.NewGuid();
            var unit = ActiveUnit(courseId, "CS101", "Intro");
            var command = BuildLecturerCommand(courseId, new[] { unit.Id });
            ArrangeHappyPath(command);
            ArrangeCourse(courseId, "Computer Science", unit);
            _offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            await CreateHandler().Handle(command, CancellationToken.None);

            // A new lecturer needs accommodation allocated exactly like a student,
            // and the approvers must be told a registration is waiting.
            _notifier.Verify(x => x.NotifyAccommodationRequiredAsync(
                It.Is<string>(n => n.Contains("Jane") && n.Contains("Smith")),
                "Lecturer",
                It.IsAny<string?>(),
                It.Is<string?>(c => c == "Computer Science"),
                It.IsAny<CancellationToken>()), Times.Once);

            _notifier.Verify(x => x.NotifyRegistrationAwaitingApprovalAsync(
                It.IsAny<string>(), "Lecturer",
                It.Is<string?>(c => c == "Computer Science"),
                It.Is<string?>(u => u != null && u.Contains("CS101")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LecturerRegistration_WhenNoActiveOffering_DoesNotFabricateOne()
        {
            var courseId = Guid.NewGuid();
            var unit = ActiveUnit(courseId, "CS101", "Intro");
            var command = BuildLecturerCommand(courseId, new[] { unit.Id });
            ArrangeHappyPath(command);
            ArrangeCourse(courseId, "CS", unit);
            _offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            await CreateHandler().Handle(command, CancellationToken.None);

            // Offering creation has its own academic-year/period uniqueness rule;
            // registration must never duplicate or race it.
            _offeringLecturerRepo.Verify(
                x => x.AddAsync(It.IsAny<CourseOfferingLecturer>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // The unit selection itself is still persisted, so staff scheduling an
            // offering later can pick it up.
            _allocationRepo.Verify(
                x => x.AddAsync(It.IsAny<UnitAllocation>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}