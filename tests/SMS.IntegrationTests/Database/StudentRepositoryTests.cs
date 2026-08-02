using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SMS.Domain.Entities;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class StudentRepositoryTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public StudentRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddAsync_ShouldAddStudent()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = new StudentRepository(context);
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var student = new Student
            {
                UserId = userId,
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks}",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Male",
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId,
                CreatedBy = "TEST"
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
            var repository = new StudentRepository(context);
            var tenantId = Guid.NewGuid();

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                TenantId = tenantId,
                CreatedBy = "TEST"
            };
            await context.Users.AddAsync(user);

            var student = new Student
            {
                UserId = user.Id,
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks}",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Male",
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId,
                CreatedBy = "TEST"
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
        public async Task GetStudentByNumberAsync_ShouldReturnStudent()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = new StudentRepository(context);
            var tenantId = Guid.NewGuid();
            var studentNumber = $"STU-{DateTime.UtcNow.Ticks}";

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@test.com",
                TenantId = tenantId,
                CreatedBy = "TEST"
            };
            await context.Users.AddAsync(user);

            var student = new Student
            {
                UserId = user.Id,
                StudentNumber = studentNumber,
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Female",
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId,
                CreatedBy = "TEST"
            };
            await repository.AddAsync(student);
            await context.SaveChangesAsync();

            // Act
            var retrieved = await repository.GetStudentByNumberAsync(studentNumber);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.StudentNumber.Should().Be(studentNumber);
            retrieved.User.FirstName.Should().Be("Jane");
        }

        [Fact]
        public async Task GetActiveStudentsAsync_ShouldReturnOnlyActiveStudents()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = new StudentRepository(context);
            var tenantId = Guid.NewGuid();

            // Create active student
            var user1 = new User { Id = Guid.NewGuid(), FirstName = "Active", LastName = "Student", Email = "active@test.com", TenantId = tenantId, CreatedBy = "TEST" };
            await context.Users.AddAsync(user1);
            var activeStudent = new Student
            {
                UserId = user1.Id,
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks}",
                DateOfBirth = new DateTime(2000, 1, 1),
                IsEnrolled = true,
                AcademicStatus = "Active",
                TenantId = tenantId,
                CreatedBy = "TEST"
            };
            await repository.AddAsync(activeStudent);

            // Create inactive student
            var user2 = new User { Id = Guid.NewGuid(), FirstName = "Inactive", LastName = "Student", Email = "inactive@test.com", TenantId = tenantId, CreatedBy = "TEST" };
            await context.Users.AddAsync(user2);
            var inactiveStudent = new Student
            {
                UserId = user2.Id,
                StudentNumber = $"STU-{DateTime.UtcNow.Ticks + 1}",
                DateOfBirth = new DateTime(2000, 1, 1),
                IsEnrolled = false,
                AcademicStatus = "Suspended",
                TenantId = tenantId,
                CreatedBy = "TEST"
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