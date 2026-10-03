using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Exceptions;
using SMS.Application.Features.Auth.Commands;
using SMS.Application.Services;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.UnitTests.Common;
using Xunit;

namespace SMS.UnitTests.Auth
{
    /// <summary>
    /// D6 regression: a failed registration left an ORPHAN Identity user.
    /// <para>
    /// Observed twice in the production-like verification: a lecturer registration
    /// rejected with "No academic period is configured..." and a registration naming
    /// a non-existent course. In both cases the AspNetUsers row and its role
    /// assignment survived with no Student/Lecturer profile behind them, and the
    /// same email then became unregistrable (409).
    /// </para>
    /// <para>
    /// Identity and the academic repositories share one ApplicationDbContext, but
    /// registration issued several SaveChangesAsync calls with no ambient
    /// transaction, so the Identity write committed independently of the academic
    /// ones. The repair runs the whole identity + academic block in one transaction
    /// and, as defence in depth, compensates by deleting the account this attempt
    /// created.
    /// </para>
    /// </summary>
    public class RegistrationFailureRollbackTests
    {
        private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SemesterId = Guid.NewGuid();
        private const string StrongPassword = "Test123!@#abcd";

        private readonly Mock<IUserManagerService> _userManager = new();
        private readonly Mock<IJwtService> _jwt = new();
        private readonly Mock<IAuditService> _audit = new();
        private readonly Mock<IUsernameGenerator> _usernameGenerator = new();
        private readonly Mock<INameParser> _nameParser = new();
        private readonly Mock<ILecturerRepository> _lecturerRepo = new();
        private readonly Mock<IStudentRepository> _studentRepo = new();
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

        private readonly string _createdUserId = Guid.NewGuid().ToString();

        private RegisterCommandHandler CreateHandler() =>
            new(
                _userManager.Object, _jwt.Object, _audit.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler>>(),
                _usernameGenerator.Object, _nameParser.Object,
                _studentRepo.Object, _lecturerRepo.Object,
                _courseRepo.Object, _unitRepo.Object, _allocationRepo.Object,
                _semesterRepo.Object, _tenantContext.Object, _unitOfWork.Object,
                new PasswordPolicyService(),
                _enrollmentRepo.Object, _offeringRepo.Object, _offeringEnrollmentRepo.Object,
                _offeringLecturerRepo.Object, _offeringUnitRepo.Object, _notifier.Object);

        private static RegisterCommand LecturerCommand(Guid courseId, IEnumerable<Guid> unitIds) =>
            new()
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Password = StrongPassword,
                ConfirmPassword = StrongPassword,
                Role = "Lecturer",
                Organization = "Test University",
                PhoneNumber = "+254711111111",
                Specialization = "Computer Science",
                CourseId = courseId,
                UnitIds = unitIds.ToList()
            };

        private static RegisterCommand StudentCommand(Guid courseId) =>
            new()
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = StrongPassword,
                ConfirmPassword = StrongPassword,
                Role = "Student",
                Organization = "Test University",
                PhoneNumber = "+254700000000",
                CourseId = courseId
            };

        /// <summary>Arranges everything up to (and including) Identity user creation.</summary>
        private void ArrangeIdentityCreated()
        {
            _tenantContext.Setup(x => x.TenantId).Returns(TenantId.ToString());
            _nameParser.Setup(x => x.ParseName(It.IsAny<string>())).Returns(new NameParseResult
            {
                FirstName = "Jane",
                LastName = "Smith",
                IsValid = true
            });
            _usernameGenerator.Setup(x => x.GenerateUsernameAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("jane.smith");

            _userManager.Setup(x => x.CreateUserAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string u, string e, string p, string r) => new User
                {
                    Id = _createdUserId,
                    Email = e,
                    UserName = u,
                    FirstName = "Jane",
                    LastName = "Smith",
                    IsActive = true
                });

            // The account really exists when the compensating cleanup looks for it.
            _userManager.Setup(x => x.FindByIdAsync(_createdUserId))
                .ReturnsAsync(new User { Id = _createdUserId, Email = "jane.smith@example.com" });
            _userManager.Setup(x => x.DeleteUserAsync(It.IsAny<string>())).ReturnsAsync(true);

            _userManager.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "Lecturer" });
            _jwt.Setup(x => x.GenerateAccessToken(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");
            _userManager.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");
        }

        private void ArrangeAcademicPeriod()
        {
            _semesterRepo.Setup(x => x.GetCurrentOrDefaultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Semester { Id = SemesterId, Name = "Semester 1", IsActive = true });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Failure AFTER the Identity user was created
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task FailedLecturerRegistration_RemovesTheIdentityUserItCreated()
        {
            // The exact production failure: no academic period is configured.
            ArrangeIdentityCreated();
            _semesterRepo.Setup(x => x.GetCurrentOrDefaultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Semester?)null!);

            var courseId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            _courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "Computer Science", Code = "CS", IsActive = true });
            _unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SMS.Domain.Entities.Unit>
                {
                    new() { Id = unitId, Code = "CS101", Name = "Unit 1", CourseId = courseId, IsActive = true }
                });

            var act = () => CreateHandler().Handle(
                LecturerCommand(courseId, new[] { unitId }), CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();

            _userManager.Verify(
                x => x.DeleteUserAsync(_createdUserId), Times.Once,
                "a failed registration must not leave an orphan Identity account behind");
        }

        [Fact]
        public async Task FailedLecturerRegistration_WithAUnitFromAnotherCourse_RemovesTheIdentityUser()
        {
            ArrangeIdentityCreated();
            ArrangeAcademicPeriod();

            var courseId = Guid.NewGuid();
            var foreignUnitId = Guid.NewGuid();
            var courseUnitId = Guid.NewGuid();
            _courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "Computer Science", Code = "CS", IsActive = true });
            // The course really owns CS101, but the registration asks for a unit that
            // is not in that set - the cross-course assignment guard.
            _unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SMS.Domain.Entities.Unit>
                {
                    new() { Id = courseUnitId, Code = "CS101", Name = "Unit 1", CourseId = courseId, IsActive = true }
                });

            var act = () => CreateHandler().Handle(
                LecturerCommand(courseId, new[] { foreignUnitId }), CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();

            _userManager.Verify(x => x.DeleteUserAsync(_createdUserId), Times.Once);
        }

        [Fact]
        public async Task FailedStudentRegistration_WithAnUnknownCourse_RemovesTheIdentityUser()
        {
            ArrangeIdentityCreated();

            var unknownCourseId = Guid.NewGuid();
            _courseRepo.Setup(x => x.GetByIdAsync(unknownCourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null!);

            var act = () => CreateHandler().Handle(
                StudentCommand(unknownCourseId), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();

            _userManager.Verify(x => x.DeleteUserAsync(_createdUserId), Times.Once);
        }

        [Fact]
        public async Task FailedRegistration_RaisesNoNotification()
        {
            ArrangeIdentityCreated();
            _semesterRepo.Setup(x => x.GetCurrentOrDefaultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Semester?)null!);

            var courseId = Guid.NewGuid();
            _courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "CS", Code = "CS", IsActive = true });

            var act = () => CreateHandler().Handle(
                LecturerCommand(courseId, new[] { Guid.NewGuid() }), CancellationToken.None);

            await act.Should().ThrowAsync<Exception>();

            _notifier.Verify(
                x => x.NotifyAccommodationRequiredAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _notifier.Verify(
                x => x.NotifyRegistrationAwaitingApprovalAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ─────────────────────────────────────────────────────────────────────
        // The happy path and the pre-Identity failures must be untouched
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SuccessfulRegistration_DoesNotRemoveAnyIdentityUser()
        {
            ArrangeIdentityCreated();
            ArrangeAcademicPeriod();

            var courseId = Guid.NewGuid();
            var unitId = Guid.NewGuid();
            _courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "Computer Science", Code = "CS", IsActive = true });
            _unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SMS.Domain.Entities.Unit>
                {
                    new() { Id = unitId, Code = "CS101", Name = "Unit 1", CourseId = courseId, IsActive = true }
                });
            _lecturerRepo.Setup(x => x.AddAsync(It.IsAny<Lecturer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Lecturer l, CancellationToken _) => l);

            var result = await CreateHandler().Handle(
                LecturerCommand(courseId, new[] { unitId }), CancellationToken.None);

            result.UserId.Should().Be(_createdUserId);
            _userManager.Verify(
                x => x.DeleteUserAsync(It.IsAny<string>()), Times.Never,
                "a completed registration must never delete its own account");
        }

        [Fact]
        public async Task FailureBeforeIdentityCreation_DeletesNothing()
        {
            // Password confirmation mismatch is rejected before any account exists.
            var command = LecturerCommand(Guid.NewGuid(), new[] { Guid.NewGuid() });
            command.ConfirmPassword = "Different1!@#abcde";

            var act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();

            _userManager.Verify(x => x.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _userManager.Verify(x => x.DeleteUserAsync(It.IsAny<string>()), Times.Never,
                "nothing was created, so nothing may be compensated");
        }

        [Fact]
        public async Task DuplicateEmail_IsRejectedWithoutDeletingTheExistingAccount()
        {
            var existingUserId = Guid.NewGuid().ToString();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new User { Id = existingUserId, Email = "jane.smith@example.com" });

            var act = () => CreateHandler().Handle(
                LecturerCommand(Guid.NewGuid(), new[] { Guid.NewGuid() }), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();

            _userManager.Verify(x => x.DeleteUserAsync(existingUserId), Times.Never,
                "an account created by an unrelated process must never be removed");
            _userManager.Verify(x => x.DeleteUserAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task FailedCompensation_DoesNotMaskTheOriginalRegistrationFailure()
        {
            ArrangeIdentityCreated();
            _semesterRepo.Setup(x => x.GetCurrentOrDefaultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Semester?)null!);

            var courseId = Guid.NewGuid();
            _courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "CS", Code = "CS", IsActive = true });

            _userManager.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("identity store unavailable"));

            var act = () => CreateHandler().Handle(
                LecturerCommand(courseId, new[] { Guid.NewGuid() }), CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>(
                "the caller must see the real reason the registration failed");
        }
    }
}
