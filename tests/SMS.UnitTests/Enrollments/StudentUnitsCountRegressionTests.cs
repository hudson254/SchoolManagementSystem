using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SMS.Application.Features.Enrollments.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;
using ITenantContext = SMS.Domain.Interfaces.ITenantContext;

namespace SMS.UnitTests.Enrollments
{
    /// <summary>
    /// D1 regression: "the student profile always reports unitsCount = 0".
    /// <para>
    /// Root cause was a READ-path gap, not a write-path gap. Registration did
    /// persist one <c>Enrollment</c> row per selected unit, but
    /// <c>GetMyPendingEnrollmentQueryHandler</c> computed
    /// <c>student.Enrollments?.Count</c> while
    /// <c>StudentRepository.GetStudentByEmailAsync</c> only included
    /// <c>SelectedCourse</c>. Lazy-loading proxies are not enabled anywhere in this
    /// application, so the navigation was an empty collection at runtime and the
    /// count was permanently zero - before AND after approval.
    /// </para>
    /// <para>
    /// Both halves are pinned here: the repository must materialize the navigation,
    /// and the handler must derive the count from the real rows.
    /// </para>
    /// </summary>
    public class StudentUnitsCountRegressionTests
    {
        private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // ─────────────────────────────────────────────────────────────────────
        // Handler level - the acceptance criteria
        // ─────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(3)]
        [InlineData(1)]
        public async Task UnitsCount_ReflectsThePersistedEnrollments(int unitCount)
        {
            var student = BuildStudent(StudentEnrollments(unitCount));

            var result = await CreateHandler(student).Handle(
                new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            result.UnitsCount.Should().Be(unitCount,
                "the count must come from the authoritative Enrollment rows, not a hard-coded value");
        }

        [Fact]
        public async Task UnitsCount_IsZero_WhenTheStudentHasNoEnrollment()
        {
            var student = BuildStudent(Array.Empty<Enrollment>());

            var result = await CreateHandler(student).Handle(
                new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            result.UnitsCount.Should().Be(0);
        }

        [Fact]
        public async Task UnitsCount_IgnoresSoftDeletedEnrollments()
        {
            var student = BuildStudent(StudentEnrollments(3));
            student.Enrollments.First().IsDeleted = true;

            var result = await CreateHandler(student).Handle(
                new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            result.UnitsCount.Should().Be(2);
        }

        [Fact]
        public async Task UnitsCount_IsTheSameBeforeAndAfterApproval()
        {
            // The reported number must describe the selection the student actually
            // submitted, so approval (which only flips Status/IsActive) cannot make
            // it drift to zero.
            var pending = BuildStudent(StudentEnrollments(3));
            pending.RegistrationStatus = RegistrationStatus.PendingApproval;
            foreach (var e in pending.Enrollments)
            {
                e.Status = "PendingApproval";
                e.IsActive = false;
            }

            var approved = BuildStudent(StudentEnrollments(3));
            approved.RegistrationStatus = RegistrationStatus.Approved;
            foreach (var e in approved.Enrollments)
            {
                e.Status = "Active";
                e.IsActive = true;
            }

            var before = await CreateHandler(pending).Handle(
                new GetMyPendingEnrollmentQuery(), CancellationToken.None);
            var after = await CreateHandler(approved).Handle(
                new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            before.UnitsCount.Should().Be(3);
            after.UnitsCount.Should().Be(3);
        }

        [Fact]
        public async Task UnitsCount_IsDerivedPerAuthenticatedStudent_NotSharedAcrossStudents()
        {
            // User scoping: the count can only ever come from THIS student's rows.
            var studentA = BuildStudent(StudentEnrollments(3));
            studentA.Email = "student.a@school.test";

            var studentB = BuildStudent(StudentEnrollments(1));
            studentB.Email = "student.b@school.test";

            var countA = await CreateHandler(studentA, "student.a@school.test").Handle(
                new GetMyPendingEnrollmentQuery(), CancellationToken.None);
            var countB = await CreateHandler(studentB, "student.b@school.test").Handle(
                new GetMyPendingEnrollmentQuery(), CancellationToken.None);

            countA.UnitsCount.Should().Be(3);
            countB.UnitsCount.Should().Be(1);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Repository level - the navigation really is loaded
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetStudentByEmailAsync_LoadsTheEnrollmentNavigation()
        {
            using var context = CreateContext();
            var course = new Course { Id = Guid.NewGuid(), Name = "Computer Science", Code = "CS01" };
            var student = BuildStudent(Array.Empty<Enrollment>());
            student.SelectedCourseId = null;
            student.SelectedCourse = course;

            context.Courses.Add(course);
            context.Students.Add(student);
            foreach (var enrollment in StudentEnrollments(3))
            {
                enrollment.StudentId = student.Id;
                enrollment.CourseId = course.Id;
                context.Enrollments.Add(enrollment);
            }
            await context.SaveChangesAsync();

            var repository = new StudentRepository(
                context, NullLoggerFactory.Instance.CreateLogger<StudentRepository>());

            var loaded = await repository.GetStudentByEmailAsync(student.Email);

            loaded.Should().NotBeNull();
            loaded!.Enrollments.Should().HaveCount(3,
                "the query must materialize the navigation the handler counts - " +
                "lazy loading is not enabled in this application");
        }

        [Fact]
        public async Task GetStudentByEmailAsync_ResolvesTheCourseNameThroughTheEnrollment()
        {
            using var context = CreateContext();
            var course = new Course { Id = Guid.NewGuid(), Name = "Computer Science", Code = "CS01" };
            var student = BuildStudent(Array.Empty<Enrollment>());
            student.SelectedCourseId = null;
            student.SelectedCourse = null;

            context.Courses.Add(course);
            context.Students.Add(student);
            context.Enrollments.Add(new Enrollment
            {
                StudentId = student.Id,
                CourseId = course.Id,
                UnitId = Guid.NewGuid(),
                Status = "PendingApproval"
            });
            await context.SaveChangesAsync();

            var repository = new StudentRepository(
                context, NullLoggerFactory.Instance.CreateLogger<StudentRepository>());

            var loaded = await repository.GetStudentByEmailAsync(student.Email);

            loaded!.Enrollments.Should().ContainSingle();
            loaded.Enrollments.Single().Course.Should().NotBeNull(
                "ThenInclude(Course) keeps the legacy course-name fallback working");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static List<Enrollment> StudentEnrollments(int count) =>
            Enumerable.Range(0, count)
                .Select(_ => new Enrollment
                {
                    StudentId = Guid.NewGuid(),
                    CourseId = Guid.NewGuid(),
                    UnitId = Guid.NewGuid(),
                    EnrollmentDate = DateTime.UtcNow,
                    Status = "PendingApproval",
                    IsActive = false
                })
                .ToList();

        private static Student BuildStudent(ICollection<Enrollment> enrollments) =>
            new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Verification",
                LastName = "Student",
                Email = "student@school.test",
                StudentNumber = "STU20261000001",
                RegistrationStatus = RegistrationStatus.PendingApproval,
                Enrollments = enrollments.ToList()
            };

        private static GetMyPendingEnrollmentQueryHandler CreateHandler(
            Student student, string? email = null)
        {
            var currentUser = new Mock<SMS.Application.Common.Interfaces.ICurrentUserService>();
            currentUser.Setup(x => x.Email).Returns(email ?? student.Email);
            currentUser.Setup(x => x.UserId).Returns(student.Id.ToString());
            currentUser.Setup(x => x.IsAuthenticated).Returns(true);

            var repository = new Mock<IStudentRepository>();
            repository.Setup(x => x.GetStudentByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(student);

            return new GetMyPendingEnrollmentQueryHandler(
                currentUser.Object,
                repository.Object,
                NullLoggerFactory.Instance.CreateLogger<GetMyPendingEnrollmentQueryHandler>());
        }

        private static ApplicationDbContext CreateContext()
        {
            var currentUser = new Mock<SMS.Domain.Interfaces.ICurrentUserService>();
            currentUser.Setup(x => x.UserId).Returns("unit-test-user");
            currentUser.Setup(x => x.Username).Returns("unit-test");
            currentUser.Setup(x => x.Email).Returns("unit-test@school.test");
            currentUser.Setup(x => x.IsAuthenticated).Returns(true);
            currentUser.Setup(x => x.Roles).Returns(new[] { "Student" });

            var tenant = new Mock<ITenantContext>();
            tenant.Setup(x => x.TenantId).Returns(TenantId.ToString());

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"UnitsCount_{Guid.NewGuid():N}")
                .Options;

            return new ApplicationDbContext(options, currentUser.Object, tenant.Object);
        }
    }
}
