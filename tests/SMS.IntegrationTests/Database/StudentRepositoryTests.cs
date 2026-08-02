using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Domain.Entities;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class StudentRepositoryTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly Mock<ILogger<StudentRepository>> _loggerMock;

        public StudentRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _loggerMock = new Mock<ILogger<StudentRepository>>();
        }

        private StudentRepository CreateRepository(ApplicationDbContext context)
        {
            return new StudentRepository(context, _loggerMock.Object);
        }

        [Fact]
        public async Task AddAsync_ShouldAddStudent()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            // ApplicationDbContext.SaveChangesAsync forces TenantId on Added ITenantAwareEntity
            // to the value from ITenantContext (mocked to this fixed ID in DatabaseFixture),
            // regardless of the value set on the entity instance.
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userId = Guid.NewGuid().ToString();

            var student = new Student
            {
                UserId = userId,
                FirstName = "Test",
                LastName = "Student",
                Email = "test.student@test.com",
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks}",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Male",
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId
            };

            // Act
            await repository.AddAsync(student);
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await repository.GetByIdAsync(student.Id);
            retrieved.Should().NotBeNull();
            retrieved!.StudentNumber.Should().Be(student.StudentNumber);
            retrieved.TenantId.Should().Be(tenantId);
        }

        [Fact]
        public async Task GetStudentWithDetailsAsync_ShouldReturnFullDetails()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var tenantId = Guid.NewGuid();

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "john.doe",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                TenantId = tenantId
            };
            await context.Users.AddAsync(user);

            var student = new Student
            {
                UserId = user.Id,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks}",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Male",
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId
            };
            await repository.AddAsync(student);
            await context.SaveChangesAsync();

            // Act
            var retrieved = await repository.GetStudentWithDetailsAsync(student.Id);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.User.Should().NotBeNull();
            retrieved.User.FirstName.Should().Be("John");
            retrieved.User.LastName.Should().Be("Doe");
        }

        [Fact]
        public async Task GetStudentByStudentNumberAsync_ShouldReturnStudent()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var tenantId = Guid.NewGuid();
            var studentNumber = $"STU-{DateTime.UtcNow.Ticks}";

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "jane.smith",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@test.com",
                TenantId = tenantId
            };
            await context.Users.AddAsync(user);

            var student = new Student
            {
                UserId = user.Id,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@test.com",
                StudentNumber = studentNumber,
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Female",
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId
            };
            await repository.AddAsync(student);
            await context.SaveChangesAsync();

            // Act
            var retrieved = await repository.GetStudentByStudentNumberAsync(studentNumber);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.StudentNumber.Should().Be(studentNumber);
        }

        [Fact]
        public async Task GetActiveStudentsAsync_ShouldReturnOnlyActiveStudents()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var tenantId = Guid.NewGuid();

            // Create active student
            var user1 = new User { Id = Guid.NewGuid().ToString(), UserName = "active", FirstName = "Active", LastName = "Student", Email = "active@test.com", TenantId = tenantId };
            await context.Users.AddAsync(user1);
            var activeStudent = new Student
            {
                UserId = user1.Id,
                FirstName = "Active",
                LastName = "Student",
                Email = "active@test.com",
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks}",
                DateOfBirth = new DateTime(2000, 1, 1),
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId
            };
            await repository.AddAsync(activeStudent);

            // Create inactive student
            var user2 = new User { Id = Guid.NewGuid().ToString(), UserName = "inactive", FirstName = "Inactive", LastName = "Student", Email = "inactive@test.com", TenantId = tenantId };
            await context.Users.AddAsync(user2);
            var inactiveStudent = new Student
            {
                UserId = user2.Id,
                FirstName = "Inactive",
                LastName = "Student",
                Email = "inactive@test.com",
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks + 1}",
                DateOfBirth = new DateTime(2000, 1, 1),
                IsEnrolled = false,
                IsActive = false,
                AcademicStatus = "Suspended",
                TenantId = tenantId
            };
            await repository.AddAsync(inactiveStudent);
            await context.SaveChangesAsync();

            // Act
            var activeStudents = await repository.GetActiveStudentsAsync();

            // Assert
            activeStudents.Should().Contain(s => s.Id == activeStudent.Id);
            activeStudents.Should().NotContain(s => s.Id == inactiveStudent.Id);
        }
    }
}
