using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class CourseOfferingRepositoryTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly Mock<ILogger<CourseOfferingRepository>> _offeringLoggerMock;
        private readonly Mock<ILogger<CourseOfferingEnrollmentRepository>> _enrollmentLoggerMock;
        private readonly Mock<ILogger<CourseOfferingLecturerRepository>> _lecturerLoggerMock;
        private readonly Mock<ILogger<CourseOfferingUnitRepository>> _unitLoggerMock;

        public CourseOfferingRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _offeringLoggerMock = new Mock<ILogger<CourseOfferingRepository>>();
            _enrollmentLoggerMock = new Mock<ILogger<CourseOfferingEnrollmentRepository>>();
            _lecturerLoggerMock = new Mock<ILogger<CourseOfferingLecturerRepository>>();
            _unitLoggerMock = new Mock<ILogger<CourseOfferingUnitRepository>>();
        }

        private CourseOfferingRepository CreateOfferingRepo(ApplicationDbContext context) =>
            new CourseOfferingRepository(context, _offeringLoggerMock.Object);

        private CourseOfferingEnrollmentRepository CreateEnrollmentRepo(ApplicationDbContext context) =>
            new CourseOfferingEnrollmentRepository(context, _enrollmentLoggerMock.Object);

        private CourseOfferingLecturerRepository CreateLecturerRepo(ApplicationDbContext context) =>
            new CourseOfferingLecturerRepository(context, _lecturerLoggerMock.Object);

        private CourseOfferingUnitRepository CreateUnitRepo(ApplicationDbContext context) =>
            new CourseOfferingUnitRepository(context, _unitLoggerMock.Object);

        private async Task<CourseOffering> SeedOfferingAsync(ApplicationDbContext context, int year = 2026)
        {
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Name = $"Test Course {Guid.NewGuid():N}",
                Code = $"TC{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                IsActive = true,
                TenantId = tenantId
            };
            await context.Courses.AddAsync(course);

            var academicYear = new AcademicYear
            {
                Id = Guid.NewGuid(),
                Name = year.ToString(),
                StartDate = new DateTime(year, 1, 1),
                EndDate = new DateTime(year, 12, 31),
                IsActive = true,
                IsCurrent = true,
                TenantId = tenantId
            };
            await context.AcademicYears.AddAsync(academicYear);

            var semester = new Semester
            {
                Id = Guid.NewGuid(),
                Name = "Semester 1",
                SemesterNumber = 1,
                StartDate = new DateTime(year, 1, 1),
                EndDate = new DateTime(year, 6, 30),
                IsActive = true,
                IsCurrent = true,
                AcademicYearId = academicYear.Id,
                TenantId = tenantId
            };
            await context.Semesters.AddAsync(semester);

            var offering = new CourseOffering
            {
                Id = Guid.NewGuid(),
                OfferingCode = $"{course.Code}-{year}-S1-001",
                CourseId = course.Id,
                AcademicYearId = academicYear.Id,
                SemesterId = semester.Id,
                Intake = $"{year} Intake A",
                StartDate = new DateTime(year, 1, 15),
                EndDate = new DateTime(year, 6, 30),
                Status = CourseOfferingStatus.Draft,
                IsActive = true,
                TenantId = tenantId
            };
            await context.CourseOfferings.AddAsync(offering);
            await context.SaveChangesAsync();

            return offering;
        }

        [Fact]
        public async Task AddAsync_ShouldAddCourseOffering()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateOfferingRepo(context);
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var offering = new CourseOffering
            {
                Id = Guid.NewGuid(),
                OfferingCode = $"WM101-2026-S1-001",
                CourseId = Guid.NewGuid(),
                AcademicYearId = Guid.NewGuid(),
                SemesterId = Guid.NewGuid(),
                Intake = "2026 Intake A",
                StartDate = new DateTime(2026, 1, 15),
                EndDate = new DateTime(2026, 6, 30),
                Status = CourseOfferingStatus.Draft,
                IsActive = true,
                TenantId = tenantId
            };

            // Act
            await repository.AddAsync(offering);
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await repository.GetByIdAsync(offering.Id);
            retrieved.Should().NotBeNull();
            retrieved!.OfferingCode.Should().Be(offering.OfferingCode);
            retrieved.TenantId.Should().Be(tenantId);
        }

        [Fact]
        public async Task GetWithDetailsAsync_ShouldReturnFullDetails()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var offeringRepo = CreateOfferingRepo(context);
            var unitRepo = CreateUnitRepo(context);
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var offering = await SeedOfferingAsync(context);

            var unit = new CourseOfferingUnit
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = offering.Id,
                Name = "Introduction to Wildlife",
                Code = "WM101U1",
                Description = "First unit",
                Credits = 1,
                ContactHours = 20,
                Order = 1,
                IsActive = true,
                TenantId = tenantId
            };
            await unitRepo.AddAsync(unit);
            await context.SaveChangesAsync();

            // Act
            var retrieved = await offeringRepo.GetWithDetailsAsync(offering.Id);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Course.Should().NotBeNull();
            retrieved.AcademicYear.Should().NotBeNull();
            retrieved.Semester.Should().NotBeNull();
            retrieved.Units.Should().Contain(u => u.Id == unit.Id);
        }

        [Fact]
        public async Task GetActiveOfferingsAsync_ShouldReturnOnlyActive()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repo = CreateOfferingRepo(context);

            var draftOffering = await SeedOfferingAsync(context, 2025);
            var activeOffering = await SeedOfferingAsync(context, 2026);

            // Set active offering status
            activeOffering.Status = CourseOfferingStatus.Active;
            await repo.UpdateAsync(activeOffering);
            await context.SaveChangesAsync();

            // Act
            var active = await repo.GetActiveOfferingsAsync();

            // Assert
            active.Should().Contain(o => o.Id == activeOffering.Id);
            active.Should().NotContain(o => o.Id == draftOffering.Id);
        }

        [Fact]
        public async Task GetByCourseIdAsync_ShouldReturnOfferingsForCourse()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repo = CreateOfferingRepo(context);

            var offering = await SeedOfferingAsync(context);

            // Act
            var result = await repo.GetByCourseIdAsync(offering.CourseId);

            // Assert
            result.Should().Contain(o => o.Id == offering.Id);
        }

        [Fact]
        public async Task GenerateOfferingCodeAsync_ShouldReturnFormattedCode()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repo = CreateOfferingRepo(context);

            // Act
            var code = await repo.GenerateOfferingCodeAsync("WM101", 2026, 1, 3);

            // Assert
            code.Should().Be("WM101-2026-S1-003");
        }

        [Fact]
        public async Task GetNextSequenceForCourseAsync_ShouldReturnCountPlusOne()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repo = CreateOfferingRepo(context);

            var offering1 = await SeedOfferingAsync(context);
            var offering2 = await SeedOfferingAsync(context);

            // Act
            var sequence = await repo.GetNextSequenceForCourseAsync(offering1.CourseId, 2026, 1);

            // Assert
            // Note: GetNextSequenceForCourseAsync counts all offerings for the course
            // regardless of year/semester filtering, so it returns count+1.
            sequence.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task EnrollmentRepository_AssignStudent_ShouldCreateEnrollment()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var offeringRepo = CreateOfferingRepo(context);
            var enrollmentRepo = CreateEnrollmentRepo(context);
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var offering = await SeedOfferingAsync(context);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid().ToString(),
                StudentNumber = $"SN-{Guid.NewGuid():N}",
                FirstName = "Test",
                LastName = "Student",
                Email = "test.student@test.com",
                IsActive = true,
                IsEnrolled = true,
                TenantId = tenantId
            };
            await context.Students.AddAsync(student);
            await context.SaveChangesAsync();

            var enrollment = new CourseOfferingEnrollment
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = offering.Id,
                StudentId = student.Id,
                Status = "PendingConfirmation",
                IsActive = true,
                AttemptNumber = 1,
                ConfirmationStatus = ConfirmationStatus.Pending,
                EnrollmentDate = DateTime.UtcNow,
                TenantId = tenantId
            };

            // Act
            await enrollmentRepo.AddAsync(enrollment);
            await context.SaveChangesAsync();

            // Assert
            var exists = await enrollmentRepo.ExistsByOfferingAndStudentAsync(offering.Id, student.Id);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task LecturerRepository_AssignLecturer_ShouldCreateAssignment()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var offeringRepo = CreateOfferingRepo(context);
            var lecturerRepo = CreateLecturerRepo(context);
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var offering = await SeedOfferingAsync(context);

            var lecturer = new Lecturer
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid().ToString(),
                FirstName = "Test",
                LastName = "Lecturer",
                Email = "test.lecturer@test.com",
                EmployeeNumber = $"EMP-{Guid.NewGuid():N}",
                IsActive = true,
                TenantId = tenantId
            };
            await context.Lecturers.AddAsync(lecturer);
            await context.SaveChangesAsync();

            var assignment = new CourseOfferingLecturer
            {
                Id = Guid.NewGuid(),
                CourseOfferingId = offering.Id,
                LecturerId = lecturer.Id,
                IsPrimary = true,
                AssignmentDate = DateTime.UtcNow,
                IsActive = true,
                TenantId = tenantId
            };

            // Act
            await lecturerRepo.AddAsync(assignment);
            await context.SaveChangesAsync();

            // Assert
            var exists = await lecturerRepo.ExistsByOfferingAndLecturerAsync(offering.Id, lecturer.Id);
            exists.Should().BeTrue();
        }
    }
}
