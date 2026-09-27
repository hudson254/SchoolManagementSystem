using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.Enrollments.Commands;
using SMS.Application.Features.ReturningUser.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.UnitTests.Enrollments
{
    /// <summary>
    /// Regression tests for the production HTTP 500 on enrollment submission.
    ///
    /// <para>
    /// <c>BaseEntity</c> pre-assigns <c>Id = Guid.NewGuid()</c>. Adding a new
    /// <see cref="Enrollment"/> through the <c>student.Enrollments</c> navigation
    /// collection made EF Core classify it <c>Modified</c> rather than
    /// <c>Added</c>, so EF issued an UPDATE for a row that does not exist and
    /// raised <c>DbUpdateConcurrencyException</c>.
    /// </para>
    ///
    /// <para>
    /// These tests pin the corrected behaviour: the repository Add path is the
    /// only sanctioned way to persist a new Enrollment, and neither handler may
    /// go back to the navigation collection.
    /// </para>
    /// </summary>
    public class EnrollmentEntityStateRegressionTests
    {
        private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private const string SubmitHandlerPath =
            "src/SMS.Application/Features/Enrollments/Commands/SubmitStudentEnrollmentCommand.cs";

        private const string ReturningHandlerPath =
            "src/SMS.Application/Features/ReturningUser/Commands/SubmitReturningStudentEnrollmentCommand.cs";

        private static ApplicationDbContext CreateContext()
        {
            var currentUser = new Mock<SMS.Domain.Interfaces.ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns("unit-test-user");
            currentUser.Setup(x => x.Username).Returns("unit-test");
            currentUser.Setup(x => x.Email).Returns("unit-test@school.com");
            currentUser.Setup(x => x.IsAuthenticated).Returns(true);
            currentUser.Setup(x => x.Roles).Returns(new[] { "Student" });

            var tenant = new Mock<ITenantContext>();
            tenant.Setup(x => x.TenantId).Returns(TenantId.ToString());
            tenant.Setup(x => x.TenantName).Returns("Unit Test Tenant");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"EnrollmentState_{Guid.NewGuid():N}")
                .Options;

            return new ApplicationDbContext(options, currentUser.Object, tenant.Object);
        }

        private static EnrollmentRepository CreateRepository(ApplicationDbContext context) =>
            new EnrollmentRepository(context, NullLoggerFactory.Instance.CreateLogger<EnrollmentRepository>());

        // ─────────────────────────────────────────────────────────────────────
        // The corrected persistence path
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RepositoryAdd_ClassifiesNewEnrollmentAsAdded_AndInsertsIt()
        {
            using var context = CreateContext();
            var repository = CreateRepository(context);

            var enrollment = new Enrollment
            {
                StudentId = Guid.NewGuid(),
                CourseId = Guid.NewGuid(),
                UnitId = Guid.NewGuid(),
                EnrollmentDate = DateTime.UtcNow,
                Status = "PendingApproval",
                IsActive = false
            };

            // The GUID is pre-assigned by BaseEntity - this is the exact condition
            // that used to cause the misclassification.
            enrollment.Id.Should().NotBe(Guid.Empty);

            await repository.AddAsync(enrollment, CancellationToken.None);
            context.ChangeTracker.DetectChanges();

            context.Entry(enrollment).State.Should().Be(
                EntityState.Added,
                "the repository Add path must mark a new Enrollment as Added, never Modified");

            var act = async () => await context.SaveChangesAsync(CancellationToken.None);
            await act.Should().NotThrowAsync("an Added entity INSERTs and must not raise a concurrency failure");

            context.Entry(enrollment).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public void NavigationCollectionAdd_IsTrackedAsModified_WhichIsWhyItIsProhibited()
        {
            using var context = CreateContext();

            var student = new Student { Email = "nav@school.com", FirstName = "Nav", LastName = "Test" };
            context.Students.Add(student);
            context.SaveChanges();

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                CourseId = Guid.NewGuid(),
                UnitId = Guid.NewGuid(),
                Status = "PendingApproval"
            };

            // The original production pattern, characterised so the reason for the
            // prohibition cannot be forgotten.
            student.Enrollments.Add(enrollment);
            context.ChangeTracker.DetectChanges();

            context.Entry(enrollment).State.Should().Be(
                EntityState.Modified,
                "a new entity discovered through a navigation collection is misclassified as Modified");
        }
        // ─────────────────────────────────────────────────────────────────────
        // Navigation regression: the handlers must not go back to the
        // navigation-collection pattern, and must route through the repository.
        // ─────────────────────────────────────────────────────────────────────

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "SchoolManagementSystem.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate the repository root (a directory containing SchoolManagementSystem.sln).");
        }

        /// <summary>
        /// Reads a handler and strips line comments, so a descriptive comment
        /// that merely mentions the forbidden call cannot mask a real one.
        /// </summary>
        private static string ReadCodeWithoutComments(string relativePath)
        {
            var fullPath = Path.Combine(FindRepositoryRoot(), relativePath);
            File.Exists(fullPath).Should().BeTrue($"{relativePath} must exist");

            var source = File.ReadAllText(fullPath);
            return Regex.Replace(source, @"//.*$", string.Empty, RegexOptions.Multiline);
        }

        [Theory]
        [InlineData(SubmitHandlerPath, "SubmitStudentEnrollmentCommandHandler")]
        [InlineData(ReturningHandlerPath, "SubmitReturningStudentEnrollmentCommandHandler")]
        public void EnrollmentHandler_DoesNotPersistThroughTheNavigationCollection(string relativePath, string handlerName)
        {
            var code = ReadCodeWithoutComments(relativePath);

            // The negative lookbehind keeps the unrelated, correct
            // _courseOfferingEnrollments.AddAsync(...) call from tripping this.
            var navigationInsert = Regex.IsMatch(code, @"(?<!CourseOffering)\bEnrollments\.Add\s*\(");

            navigationInsert.Should().BeFalse(
                $"{handlerName} must not add a new Enrollment through the student navigation collection: " +
                "a pre-generated Guid makes EF classify the entity as Modified and the save fails with " +
                "DbUpdateConcurrencyException (HTTP 500)");

            code.Should().Contain(
                "_enrollmentRepository.AddAsync",
                $"{handlerName} must persist new Enrollments through the injected repository");
        }

        [Theory]
        [InlineData(SubmitHandlerPath)]
        [InlineData(ReturningHandlerPath)]
        public void EnrollmentHandler_LeavesBaseEntityGuidGenerationAlone(string relativePath)
        {
            // The repair must not "solve" the problem by removing the GUID key.
            var baseEntity = File.ReadAllText(
                Path.Combine(FindRepositoryRoot(), "src/SMS.Domain/Common/BaseEntity.cs"));

            baseEntity.Should().Contain("Guid.NewGuid()",
                "BaseEntity must keep pre-assigning its GUID primary key");
        }
        // ─────────────────────────────────────────────────────────────────────
        // Handler-level proof that persistence goes through the repository
        // ─────────────────────────────────────────────────────────────────────

        private static Mock<SMS.Application.Common.Interfaces.ICurrentUserService> CurrentUserFor(string email)
        {
            var mock = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
            mock.Setup(x => x.UserId).Returns("unit-test-user");
            mock.Setup(x => x.Username).Returns("unit-test");
            mock.Setup(x => x.Email).Returns(email);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.Roles).Returns(new[] { "Student" });
            return mock;
        }

        private static List<Unit> SampleUnits(Guid courseId) => new()
        {
            new Unit { Code = "U1", Name = "Unit One", CourseId = courseId, IsActive = true },
            new Unit { Code = "U2", Name = "Unit Two", CourseId = courseId, IsActive = true }
        };

        [Fact]
        public async Task SubmitStudentEnrollmentCommand_PersistsEachUnitThroughTheEnrollmentRepository()
        {
            var email = "submit@school.com";
            var student = new Student
            {
                Email = email,
                FirstName = "Sub",
                LastName = "Mit",
                RegistrationStatus = RegistrationStatus.PendingCourseSelection,
                SelectedCourseId = Guid.NewGuid()
            };
            var course = new Course { Name = "Course", Code = "C1", IsActive = true };
            student.SelectedCourseId = course.Id;
            var units = SampleUnits(course.Id);

            var students = new Mock<IStudentRepository>();
            students.Setup(x => x.GetStudentByEmailAsync(email)).ReturnsAsync(student);

            var courses = new Mock<ICourseRepository>();
            courses.Setup(x => x.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            var unitRepo = new Mock<IUnitRepository>();
            unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(units);

            var enrollments = new Mock<IEnrollmentRepository>();
            var offerings = new Mock<ICourseOfferingRepository>();
            offerings.Setup(x => x.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CourseOffering>());

            var offeringEnrollments = new Mock<ICourseOfferingEnrollmentRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = new SubmitStudentEnrollmentCommandHandler(
                CurrentUserFor(email).Object,
                students.Object,
                courses.Object,
                unitRepo.Object,
                enrollments.Object,
                offerings.Object,
                offeringEnrollments.Object,
                new Mock<IAuditService>().Object,
                unitOfWork.Object,
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
                    .CreateLogger<SubmitStudentEnrollmentCommandHandler>());

            var result = await handler.Handle(
                new SubmitStudentEnrollmentCommand { CourseId = course.Id },
                CancellationToken.None);

            // Persistence happened through the repository, once per active unit.
            enrollments.Verify(
                x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
                Times.Exactly(units.Count));

            var added = new List<Enrollment>();
            enrollments.Verify(
                x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
                Times.Exactly(units.Count));
            foreach (var invocation in enrollments.Invocations
                         .Where(i => i.Method.Name == nameof(IEnrollmentRepository.AddAsync)))
            {
                added.Add((Enrollment)invocation.Arguments[0]);
            }

            added.Should().HaveCount(2);
            added.Should().OnlyContain(e => e.Status == "PendingApproval");
            added.Should().OnlyContain(e => e.IsActive == false);
            added.Select(e => e.UnitId).Should().BeEquivalentTo(units.Select(u => u.Id));

            // The navigation collection is NOT the persistence mechanism any more.
            student.Enrollments.Should().BeEmpty(
                "the handler must not attach new Enrollments to the student navigation collection");

            // Existing behaviour from 5dc711f9 is preserved.
            result.Status.Should().Be("PendingApproval");
            result.UnitsEnrolled.Should().Be(2);
            student.SelectedCourseId.Should().Be(course.Id);
            student.RegistrationStatus.Should().Be(RegistrationStatus.PendingApproval);

            unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task SubmitReturningStudentEnrollmentCommand_PersistsEachUnitThroughTheEnrollmentRepository()
        {
            var email = "returning@school.com";
            var course = new Course { Name = "Course", Code = "C2", IsActive = true };
            var semesterId = Guid.NewGuid();

            // A returning student is Approved.
            var student = new Student
            {
                Email = email,
                FirstName = "Re",
                LastName = "Turn",
                RegistrationStatus = RegistrationStatus.Approved,
                SelectedCourseId = course.Id
            };
            var units = SampleUnits(course.Id);

            var students = new Mock<IStudentRepository>();
            students.Setup(x => x.GetStudentByEmailAsync(email)).ReturnsAsync(student);

            var courses = new Mock<ICourseRepository>();
            courses.Setup(x => x.GetByIdAsync(course.Id, It.IsAny<CancellationToken>())).ReturnsAsync(course);

            var unitRepo = new Mock<IUnitRepository>();
            unitRepo.Setup(x => x.GetUnitsByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(units);

            var enrollments = new Mock<IEnrollmentRepository>();
            enrollments.Setup(x => x.IsStudentEnrolledAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(false);

            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = new SubmitReturningStudentEnrollmentCommandHandler(
                CurrentUserFor(email).Object,
                students.Object,
                courses.Object,
                unitRepo.Object,
                enrollments.Object,
                new Mock<IAuditService>().Object,
                unitOfWork.Object,
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
                    .CreateLogger<SubmitReturningStudentEnrollmentCommandHandler>());

            var result = await handler.Handle(
                new SubmitReturningStudentEnrollmentCommand
                {
                    CourseId = course.Id,
                    SemesterId = semesterId
                },
                CancellationToken.None);

            enrollments.Verify(
                x => x.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
                Times.Exactly(units.Count));

            var added = new List<Enrollment>();
            foreach (var invocation in enrollments.Invocations
                         .Where(i => i.Method.Name == nameof(IEnrollmentRepository.AddAsync)))
            {
                added.Add((Enrollment)invocation.Arguments[0]);
            }

            added.Should().HaveCount(2);
            added.Should().OnlyContain(e => e.Status == "Active");
            added.Should().OnlyContain(e => e.SemesterId == semesterId);

            student.Enrollments.Should().BeEmpty(
                "the returning-student path must not use the navigation collection either");

            result.Status.Should().Be("Active");
            result.UnitsEnrolled.Should().Be(2);
            student.CurrentSemesterId.Should().Be(semesterId);

            unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
