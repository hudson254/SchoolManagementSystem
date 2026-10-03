using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.Auth.Commands;
using SMS.Application.Features.Dashboard.Queries;
using SMS.Application.Features.Enrollments.Queries;
using SMS.Application.Services;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Enrollments
{
    /// <summary>
    /// Regression tests for the reported production defect: "the course selected
    /// during account creation does not automatically reflect on the student
    /// details and dashboard."
    ///
    /// <para>
    /// <c>RegisterCommand.CreateStudentRecord</c> validated <c>CourseId</c>, loaded
    /// the course, then built the <c>Student</c> without persisting it. The
    /// downstream readers consequently had nothing to show:
    /// <c>GetMyPendingEnrollmentQuery</c> derived the course from
    /// <c>student.Enrollments</c> (empty after registration) and computed
    /// <c>HasSelectedCourse = !needsCourseSelection</c>, always false while the
    /// student is in <c>PendingCourseSelection</c>, and
    /// <c>GetMyStudentDashboardQuery</c> built its cards exclusively from
    /// <c>course_offering_enrollments</c>, which registration never creates.
    /// </para>
    /// </summary>
    public class StudentCourseSelectionPersistenceTests
    {
        private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // ─────────────────────────────────────────────────────────────────────
        // RegisterCommand persists SelectedCourseId
        // ─────────────────────────────────────────────────────────────────────
        private static (
            RegisterCommandHandler Handler,
            Mock<IStudentRepository> StudentRepo,
            Mock<ICourseRepository> CourseRepo,
            Mock<IUserManagerService> UserManager,
            Mock<IJwtService> Jwt,
            Mock<IUsernameGenerator> UsernameGenerator,
            Mock<INameParser> NameParser,
            Mock<IEnrollmentRepository> EnrollmentRepo,
            Mock<ICourseOfferingRepository> OfferingRepo,
            Mock<ICourseOfferingEnrollmentRepository> OfferingEnrollmentRepo,
            Mock<ICourseOfferingLecturerRepository> OfferingLecturerRepo,
            Mock<ICourseOfferingUnitRepository> OfferingUnitRepo,
            Mock<IUnitRepository> UnitRepo,
            Mock<IUnitAllocationRepository> UnitAllocationRepo,
            Mock<IBusinessEventNotifier> Notifier) BuildRegisterHandler()
        {
            var userManager = new Mock<IUserManagerService>();
            var jwt = new Mock<IJwtService>();
            var audit = new Mock<IAuditService>();
            var usernameGenerator = new Mock<IUsernameGenerator>();
            var nameParser = new Mock<INameParser>();
            var studentRepo = new Mock<IStudentRepository>();
            var lecturerRepo = new Mock<ILecturerRepository>();
            var courseRepo = new Mock<ICourseRepository>();
            var unitRepo = new Mock<IUnitRepository>();
            var unitAllocationRepo = new Mock<IUnitAllocationRepository>();
            var tenantContext = new Mock<SMS.Multitenancy.Interfaces.ITenantContext>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var enrollmentRepo = new Mock<IEnrollmentRepository>();
            var courseOfferingRepo = new Mock<ICourseOfferingRepository>();
            var courseOfferingEnrollmentRepo = new Mock<ICourseOfferingEnrollmentRepository>();
            var courseOfferingLecturerRepo = new Mock<ICourseOfferingLecturerRepository>();
            var courseOfferingUnitRepo = new Mock<ICourseOfferingUnitRepository>();
            var notifier = new Mock<IBusinessEventNotifier>();

            tenantContext.Setup(x => x.TenantId).Returns(TenantId.ToString());

            var handler = new RegisterCommandHandler(
                userManager.Object,
                jwt.Object,
                audit.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler>>(),
                usernameGenerator.Object,
                nameParser.Object,
                studentRepo.Object,
                lecturerRepo.Object,
                courseRepo.Object,
                unitRepo.Object,
                unitAllocationRepo.Object,
                new Mock<ISemesterRepository>().Object,
                tenantContext.Object,
                unitOfWork.Object,
                new PasswordPolicyService(),
                enrollmentRepo.Object,
                courseOfferingRepo.Object,
                courseOfferingEnrollmentRepo.Object,
                courseOfferingLecturerRepo.Object,
                courseOfferingUnitRepo.Object,
                notifier.Object);

            return (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, courseOfferingRepo,
                courseOfferingEnrollmentRepo, courseOfferingLecturerRepo, courseOfferingUnitRepo,
                unitRepo, unitAllocationRepo, notifier);
        }

        private static RegisterCommand BuildStudentCommand(Guid courseId) => new RegisterCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Test123!@#abcd",
            ConfirmPassword = "Test123!@#abcd",
            Role = "Student",
            Organization = "Test University",
            PhoneNumber = "+254700000000",
            CourseId = courseId
        };

        private static void ArrangeHappyPath(
            RegisterCommand command,
            Mock<IUserManagerService> userManager,
            Mock<IJwtService> jwt,
            Mock<IUsernameGenerator> usernameGenerator,
            Mock<INameParser> nameParser,
            string userId)
        {
            userManager.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null!);
            nameParser.Setup(x => x.ParseName(It.IsAny<string>())).Returns(new NameParseResult
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                IsValid = true
            });
            usernameGenerator.Setup(x => x.GenerateUsernameAsync("John", "Doe")).ReturnsAsync("john.doe");
            userManager
                .Setup(x => x.CreateUserAsync("john.doe", command.Email, command.Password, command.Role))
                .ReturnsAsync((string username, string email, string password, string role) => new User
                {
                    Id = userId,
                    Email = email,
                    UserName = username,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    IsActive = true
                });
            userManager.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "Student" });
            jwt.Setup(x => x.GenerateAccessToken(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");
            userManager.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");
        }
        [Fact]
        public async Task RegisterStudent_PersistsTheSelectedCourseIdOnTheStudentRecord()
        {
            var courseId = Guid.NewGuid();
            var programmeId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo, offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course
                {
                    Id = courseId,
                    Name = "Computer Science",
                    Code = "CS101",
                    IsActive = true,
                    ProgrammeId = programmeId
                });

            Student? capturedStudent = null;
            studentRepo
                .Setup(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                .Callback<Student, CancellationToken>((s, _) => capturedStudent = s)
                .ReturnsAsync((Student s, CancellationToken _) => s);

            await handler.Handle(command, CancellationToken.None);

            capturedStudent.Should().NotBeNull();
            capturedStudent!.SelectedCourseId.Should().Be(courseId,
                "the course chosen at registration must be persisted, not just validated");
            // The course WAS selected during this registration, so the account is no
            // longer "awaiting course selection". PendingCourseSelection used to be
            // written here, which made the registration invisible to
            // GetPendingApprovalsQuery (filters on PendingApproval) and made
            // ApproveRegistrationCommand throw - an approval dead-end no admin could
            // clear. PendingApproval is the state the existing approval workflow
            // actually consumes.
            capturedStudent.RegistrationStatus.Should().Be(RegistrationStatus.PendingApproval);
            capturedStudent.IsEnrolled.Should().BeFalse();
            capturedStudent.ProgrammeId.Should().Be(programmeId);
        }

        [Fact]
        public async Task RegisterStudent_FallsBackToCourseProgrammes_WhenCourseProgrammeIdIsNull()
        {
            // A fresh production DB where courses have no programme linked is
            // exactly the case that made ProgrammeId null as well.
            var courseId = Guid.NewGuid();
            var fallbackProgrammeId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo, offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            var course = new Course
            {
                Id = courseId,
                Name = "Computer Science",
                Code = "CS101",
                IsActive = true,
                ProgrammeId = null
            };
            course.Programmes.Add(new Programme { Id = fallbackProgrammeId });

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            Student? capturedStudent = null;
            studentRepo
                .Setup(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                .Callback<Student, CancellationToken>((s, _) => capturedStudent = s)
                .ReturnsAsync((Student s, CancellationToken _) => s);

            await handler.Handle(command, CancellationToken.None);

            capturedStudent.Should().NotBeNull();
            capturedStudent!.SelectedCourseId.Should().Be(courseId);
            capturedStudent.ProgrammeId.Should().Be(fallbackProgrammeId);
        }
        [Fact]
        public async Task RegisterStudent_LeavesProgrammeIdNull_WhenTheCourseHasNoProgrammeAtAll()
        {
            // A course that is linked to no programme in either direction. The
            // fallback must leave the FK NULL: FirstOrDefault() over a
            // non-nullable Guid sequence yields Guid.Empty, which is not null
            // and violates the Students -> Programmes foreign key at save time
            // (a 500 on POST /register). This test pins that distinction.
            var courseId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo, offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course
                {
                    Id = courseId,
                    Name = "Unprogrammed Course",
                    Code = "UNP101",
                    IsActive = true,
                    ProgrammeId = null
                });

            Student? capturedStudent = null;
            studentRepo
                .Setup(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                .Callback<Student, CancellationToken>((s, _) => capturedStudent = s)
                .ReturnsAsync((Student s, CancellationToken _) => s);

            await handler.Handle(command, CancellationToken.None);

            capturedStudent.Should().NotBeNull();
            capturedStudent!.SelectedCourseId.Should().Be(courseId,
                "the course selection is still persisted even without a programme");
            capturedStudent.ProgrammeId.Should().BeNull(
                "Guid.Empty is not an acceptable substitute for a null programme FK");
            capturedStudent.ProgrammeId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task RegisterStudent_WithoutCourseId_ShouldThrowValidationException()
        {
            var (handler, _, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo, offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(Guid.NewGuid());
            command.CourseId = null;
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            await Assert.ThrowsAsync<ValidationException>(
                () => handler.Handle(command, CancellationToken.None));

            courseRepo.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterStudent_WithInactiveCourse_ShouldThrowNotFoundException()
        {
            var courseId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo, offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course
                {
                    Id = courseId,
                    Name = "Retired Course",
                    Code = "OLD101",
                    IsActive = false
                });

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));

            studentRepo.Verify(
                x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "an inactive course must never be persisted as a selection");
        }

        [Fact]
        public async Task RegisterStudent_PersistsAnEnrollmentRowPerSelectedUnit_SoUnitsAreImmediatelyRetrievable()
        {
            var courseId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo,
                offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            var units = new List<SMS.Domain.Entities.Unit>
            {
                new() { Id = Guid.NewGuid(), Code = "CS101", Name = "Intro", CourseId = courseId, IsActive = true },
                new() { Id = Guid.NewGuid(), Code = "CS102", Name = "Logic", CourseId = courseId, IsActive = true },
                new() { Id = Guid.NewGuid(), Code = "CS999", Name = "Retired", CourseId = courseId, IsActive = false }
            };

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "CS", Code = "CSC", IsActive = true });
            unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(units);

            Guid? capturedStudentId = null;
            studentRepo.Setup(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                .Callback<Student, CancellationToken>((s, _) => capturedStudentId = s.Id)
                .ReturnsAsync((Student s, CancellationToken _) => s);

            var saved = new List<Enrollment>();
            enrollmentRepo.Setup(x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
                .Callback<Enrollment, CancellationToken>((e, _) => saved.Add(e))
                .ReturnsAsync((Enrollment e, CancellationToken _) => e);

            await handler.Handle(command, CancellationToken.None);

            // Only ACTIVE units are enrolled - an inactive unit must never be.
            saved.Should().HaveCount(2);
            saved.Select(e => e.UnitId).Should().BeEquivalentTo(
                units.Where(u => u.IsActive).Select(u => u.Id));

            // Every row points at the persisted student, so the enrollment is not
            // an orphan and the dashboard/status queries can join it.
            saved.Should().OnlyContain(e => e.StudentId == capturedStudentId!.Value);
            saved.Should().OnlyContain(e => e.CourseId == courseId);

            // PendingApproval/inactive matches the existing enrollment command and is
            // exactly what ApproveRegistrationCommand later flips to Active.
            saved.Should().OnlyContain(e => e.Status == "PendingApproval");
            saved.Should().OnlyContain(e => e.IsActive == false);
        }

        [Fact]
        public async Task RegisterStudent_WhenActiveOfferingExists_CreatesPendingCourseOfferingEnrollment()
        {
            var courseId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo,
                offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "CS", Code = "CSC", IsActive = true });
            unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SMS.Domain.Entities.Unit>
                {
                    new() { Id = Guid.NewGuid(), Code = "CS101", Name = "Intro", CourseId = courseId, IsActive = true }
                });

            // Exactly one active offering -> it is the unambiguous match.
            offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering> { new() { Id = offeringId, CourseId = courseId, IsActive = true } });
            offeringEnrollmentRepo
                .Setup(x => x.ExistsByOfferingAndStudentAsync(offeringId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var created = new List<CourseOfferingEnrollment>();
            offeringEnrollmentRepo.Setup(x => x.AddAsync(It.IsAny<CourseOfferingEnrollment>(), It.IsAny<CancellationToken>()))
                .Callback<CourseOfferingEnrollment, CancellationToken>((e, _) => created.Add(e))
                .ReturnsAsync((CourseOfferingEnrollment e, CancellationToken _) => e);

            await handler.Handle(command, CancellationToken.None);

            created.Should().HaveCount(1);
            created[0].CourseOfferingId.Should().Be(offeringId);

            // PendingConfirmation keeps it out of GetActiveByStudentAsync until an
            // administrator confirms - the pre-existing contract, unchanged.
            created[0].Status.Should().Be("PendingConfirmation");
            created[0].ConfirmationStatus.Should().Be(ConfirmationStatus.Pending);
        }

        [Fact]
        public async Task RegisterStudent_WhenNoActiveOffering_RecordsSelectionWithoutFabricatingOne()
        {
            var courseId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo,
                offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "CS", Code = "CSC", IsActive = true });
            unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SMS.Domain.Entities.Unit>
                {
                    new() { Id = Guid.NewGuid(), Code = "CS101", Name = "Intro", CourseId = courseId, IsActive = true }
                });
            offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            Student? captured = null;
            studentRepo.Setup(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                .Callback<Student, CancellationToken>((s, _) => captured = s)
                .ReturnsAsync((Student s, CancellationToken _) => s);

            await handler.Handle(command, CancellationToken.None);

            // The selection is still authoritative on the student record...
            captured!.SelectedCourseId.Should().Be(courseId);

            // ...but registration must NOT invent a course offering: offering
            // creation carries its own academic-year/period uniqueness rule.
            offeringEnrollmentRepo.Verify(
                x => x.AddAsync(It.IsAny<CourseOfferingEnrollment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterStudent_RaisesAccommodationAndApprovalNotifications()
        {
            var courseId = Guid.NewGuid();
            var (handler, studentRepo, courseRepo, userManager, jwt,
                usernameGenerator, nameParser, enrollmentRepo, offeringRepo, offeringEnrollmentRepo,
                offeringLecturerRepo, offeringUnitRepo, unitRepo, unitAllocationRepo, notifier) = BuildRegisterHandler();

            var command = BuildStudentCommand(courseId);
            ArrangeHappyPath(command, userManager, jwt, usernameGenerator,
                nameParser, Guid.NewGuid().ToString());

            courseRepo.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course { Id = courseId, Name = "Computer Science", Code = "CS101", IsActive = true });
            unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SMS.Domain.Entities.Unit>
                {
                    new() { Id = Guid.NewGuid(), Code = "CS101", Name = "Intro", CourseId = courseId, IsActive = true }
                });
            offeringRepo.Setup(x => x.GetByCourseIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            await handler.Handle(command, CancellationToken.None);

            // Accommodation allocation is required for every new account, so the
            // back-office roles must be told - naming the person and the course.
            notifier.Verify(x => x.NotifyAccommodationRequiredAsync(
                It.Is<string>(n => n.Contains("John") && n.Contains("Doe")),
                "Student",
                It.IsAny<string?>(),
                It.Is<string?>(c => c == "Computer Science"),
                It.IsAny<CancellationToken>()), Times.Once);

            notifier.Verify(x => x.NotifyRegistrationAwaitingApprovalAsync(
                It.IsAny<string>(), "Student",
                It.Is<string?>(c => c == "Computer Science"),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetMyPendingEnrollmentQuery reads the persisted selection
        // ─────────────────────────────────────────────────────────────────────

        private static (GetMyPendingEnrollmentQueryHandler Handler, Mock<IStudentRepository> Repo)
            BuildPendingHandler(Student student)
        {
            var currentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
            currentUser.Setup(x => x.Email).Returns(student.Email);

            var repo = new Mock<IStudentRepository>();
            repo.Setup(x => x.GetStudentByEmailAsync(student.Email)).ReturnsAsync(student);

            var handler = new GetMyPendingEnrollmentQueryHandler(
                currentUser.Object,
                repo.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetMyPendingEnrollmentQueryHandler>>());

            return (handler, repo);
        }
        [Fact]
        public async Task PendingEnrollment_WithSelectedCourseId_ReportsHasSelectedCourseAndName()
        {
            var courseId = Guid.NewGuid();
            var student = new Student
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                StudentNumber = "STU202601010001",
                RegistrationStatus = RegistrationStatus.PendingCourseSelection,
                SelectedCourseId = courseId,
                SelectedCourse = new Course
                {
                    Id = courseId,
                    Name = "Computer Science",
                    Code = "CS101"
                }
            };

            var (handler, _) = BuildPendingHandler(student);

            var result = await handler.Handle(new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            result.HasSelectedCourse.Should().BeTrue(
                "a course persisted at registration must be reported as selected");
            result.SelectedCourseId.Should().Be(courseId);
            result.SelectedCourseName.Should().Be("Computer Science");
            result.RegistrationStatus.Should().Be("PendingCourseSelection");
            // Consistency: a persisted selection means the student does not need
            // to select a course again.
            result.NeedsCourseSelection.Should().BeFalse();
        }

        [Fact]
        public async Task PendingEnrollment_WithoutSelectedCourseId_ReportsHasSelectedCourseFalse()
        {
            var student = new Student
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                StudentNumber = "STU202601010002",
                RegistrationStatus = RegistrationStatus.PendingCourseSelection,
                SelectedCourseId = null
            };

            var (handler, _) = BuildPendingHandler(student);

            var result = await handler.Handle(new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            result.HasSelectedCourse.Should().BeFalse();
            result.SelectedCourseId.Should().BeNull();
            result.SelectedCourseName.Should().BeNull();
            result.NeedsCourseSelection.Should().BeTrue();
        }

        [Fact]
        public async Task PendingEnrollment_WhenApproved_ReportsHasSelectedCourseTrueAndDoesNotNeedSelection()
        {
            var courseId = Guid.NewGuid();
            var student = new Student
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                StudentNumber = "STU202601010003",
                RegistrationStatus = RegistrationStatus.Approved,
                SelectedCourseId = courseId,
                SelectedCourse = new Course { Id = courseId, Name = "Computer Science", Code = "CS101" }
            };

            var (handler, _) = BuildPendingHandler(student);

            var result = await handler.Handle(new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            result.IsApproved.Should().BeTrue();
            result.HasSelectedCourse.Should().BeTrue();
            result.NeedsCourseSelection.Should().BeFalse();
        }
        // ─────────────────────────────────────────────────────────────────────
        // GetMyStudentDashboardQuery surfaces the pending course card
        // ─────────────────────────────────────────────────────────────────────

        private static (GetMyStudentDashboardQueryHandler Handler, Mock<ICourseRepository> CourseRepo,
            Mock<ICourseOfferingRepository> Offerings)
            BuildDashboardHandler(Student student,
                IEnumerable<CourseOfferingEnrollment>? enrollments = null)
        {
            var access = new Mock<IAcademicAccessService>();
            access.Setup(x => x.GetCurrentStudentAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(student);

            var offeringEnrollments = new Mock<ICourseOfferingEnrollmentRepository>();
            offeringEnrollments
                .Setup(x => x.GetActiveByStudentAsync(student.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollments ?? Enumerable.Empty<CourseOfferingEnrollment>());

            var offerings = new Mock<ICourseOfferingRepository>();
            var offeringUnits = new Mock<ICourseOfferingUnitRepository>();
            var courses = new Mock<ICourseRepository>();
            var accommodations = new Mock<IAccommodationRepository>();
            accommodations
                .Setup(x => x.GetAssignmentByStudentAsync(student.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.AccommodationAssignment)null!);

            var handler = new GetMyStudentDashboardQueryHandler(
                access.Object,
                offeringEnrollments.Object,
                offerings.Object,
                offeringUnits.Object,
                courses.Object,
                accommodations.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetMyStudentDashboardQueryHandler>>());

            return (handler, courses, offerings);
        }

        [Fact]
        public async Task StudentDashboard_WithSelectedCourseAndNoEnrollments_ReturnsPendingCourseCard()
        {
            var courseId = Guid.NewGuid();
            var student = new Student
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                StudentNumber = "STU202601010004",
                RegistrationStatus = RegistrationStatus.PendingCourseSelection,
                SelectedCourseId = courseId
            };

            var (handler, courses, _) = BuildDashboardHandler(student);
            courses.Setup(x => x.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course
                {
                    Id = courseId,
                    Name = "Computer Science",
                    Code = "CS101",
                    Description = "CS degree course"
                });

            var result = await handler.Handle(new GetMyStudentDashboardQuery(), CancellationToken.None);

            result.PendingCourse.Should().NotBeNull(
                "a persisted selection must be visible on the dashboard");
            result.PendingCourse!.CourseId.Should().Be(courseId);
            result.PendingCourse.CourseName.Should().Be("Computer Science");
            result.PendingCourse.CourseCode.Should().Be("CS101");
            result.PendingCourse.Status.Should().Be("PendingSelection");
            result.PendingCourse.RequiresSubmission.Should().BeTrue();
            result.PendingCourse.RegistrationStatus.Should().Be("PendingCourseSelection");
            result.Enrollments.Should().BeEmpty();
        }

        [Fact]
        public async Task StudentDashboard_WithoutSelectedCourse_ReturnsNoPendingCard()
        {
            var student = new Student
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                StudentNumber = "STU202601010005",
                RegistrationStatus = RegistrationStatus.PendingCourseSelection,
                SelectedCourseId = null
            };

            var (handler, _, _) = BuildDashboardHandler(student);

            var result = await handler.Handle(new GetMyStudentDashboardQuery(), CancellationToken.None);

            result.PendingCourse.Should().BeNull();
            result.Enrollments.Should().BeEmpty();
        }
        [Fact]
        public async Task StudentDashboard_StillReturnsOfferingBackedCourseCards_WhenTheyExist()
        {
            // Regression guard: existing behaviour for students with real active
            // course_offering_enrollments must not change.
            var offeringCourseId = Guid.NewGuid();
            var selectedCourseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();

            var student = new Student
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                StudentNumber = "STU202601010006",
                RegistrationStatus = RegistrationStatus.Approved,
                // A different/stale selection alongside an approved enrollment.
                SelectedCourseId = selectedCourseId
            };

            var enrollment = new CourseOfferingEnrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseOfferingId = offeringId,
                Status = "Active",
                IsActive = true,
                ConfirmationStatus = ConfirmationStatus.Confirmed,
                AttemptNumber = 1
            };

            var (handler, courses, offerings) =
                BuildDashboardHandler(student, new[] { enrollment });

            offerings.Setup(x => x.GetWithDetailsAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering
                {
                    Id = offeringId,
                    CourseId = offeringCourseId,
                    OfferingCode = "CS-2026-S1-001",
                    AcademicYearName = "2026/2027",
                    SemesterName = "Semester 1",
                    Course = new Course
                    {
                        Id = offeringCourseId,
                        Name = "Computer Science",
                        Code = "CS101"
                    }
                });

            // The selected course is a DIFFERENT course from the enrolled one,
            // so it must still surface as a pending card alongside the
            // offering-backed card.
            courses.Setup(x => x.GetByIdAsync(selectedCourseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Course
                {
                    Id = selectedCourseId,
                    Name = "Business Administration",
                    Code = "BA201"
                });

            var result = await handler.Handle(new GetMyStudentDashboardQuery(), CancellationToken.None);

            result.Enrollments.Should().HaveCount(1);
            var card = result.Enrollments[0];
            card.CourseOfferingId.Should().Be(offeringId);
            card.CourseId.Should().Be(offeringCourseId);
            card.CourseName.Should().Be("Computer Science");
            card.CourseCode.Should().Be("CS101");
            card.OfferingCode.Should().Be("CS-2026-S1-001");
            card.AcademicYearName.Should().Be("2026/2027");
            card.SemesterName.Should().Be("Semester 1");
            card.Status.Should().Be("Active");
            card.ConfirmationStatus.Should().Be("Confirmed");
            card.AttemptNumber.Should().Be(1);

            result.PendingCourse.Should().NotBeNull();
            result.PendingCourse!.CourseCode.Should().Be("BA201");
            result.PendingCourse.Status.Should().Be("PendingApproval");
            result.PendingCourse.RequiresSubmission.Should().BeFalse();
        }

        [Fact]
        public async Task StudentDashboard_SuppressesPendingCard_WhenAlreadyEnrolledInTheSelectedCourse()
        {
            var courseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var offeringId = Guid.NewGuid();

            var student = new Student
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                StudentNumber = "STU202601010007",
                RegistrationStatus = RegistrationStatus.Approved,
                SelectedCourseId = courseId
            };

            var enrollment = new CourseOfferingEnrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseOfferingId = offeringId,
                Status = "Active",
                IsActive = true
            };

            var (handler, courses, offerings) =
                BuildDashboardHandler(student, new[] { enrollment });

            offerings.Setup(x => x.GetWithDetailsAsync(offeringId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CourseOffering
                {
                    Id = offeringId,
                    CourseId = courseId,
                    OfferingCode = "CS-2026-S1-002",
                    AcademicYearName = "2026/2027",
                    SemesterName = "Semester 1",
                    Course = new Course { Id = courseId, Name = "Computer Science", Code = "CS101" }
                });

            var result = await handler.Handle(new GetMyStudentDashboardQuery(), CancellationToken.None);

            result.Enrollments.Should().HaveCount(1);
            result.PendingCourse.Should().BeNull(
                "the same course must not be shown twice on the dashboard");

            courses.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
