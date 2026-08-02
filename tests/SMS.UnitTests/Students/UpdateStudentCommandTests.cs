using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SMS.Application.Exceptions;
using SMS.Application.Features.Students.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Students
{
    public class UpdateStudentCommandTests
    {
        private readonly UpdateStudentCommandValidator _validator;
        private readonly Mock<IStudentRepository> _studentRepositoryMock;
        private readonly Mock<IUserManagerService> _userManagerMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public UpdateStudentCommandTests()
        {
            _validator = new UpdateStudentCommandValidator();
            _studentRepositoryMock = new Mock<IStudentRepository>();
            _userManagerMock = new Mock<IUserManagerService>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new UpdateStudentCommand
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1),
                IsActive = true
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            // Arrange
            var command = new UpdateStudentCommand
            {
                Id = Guid.Empty,
                FirstName = "",
                LastName = "",
                PhoneNumber = ""
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id);
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
        }

        [Fact]
        public async Task Handle_WithNonExistentStudent_ShouldThrowNotFoundException()
        {
            // Arrange
            var command = new UpdateStudentCommand
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1),
                IsActive = true
            };

            _studentRepositoryMock
                .Setup(x => x.GetStudentWithDetailsAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Student?)null);

            var handler = new UpdateStudentCommandHandler(
                _studentRepositoryMock.Object,
                _userManagerMock.Object,
                _auditServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldUpdateStudent()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var command = new UpdateStudentCommand
            {
                Id = studentId,
                FirstName = "John",
                LastName = "Updated",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1),
                IsActive = true
            };

            var existingStudent = new Student
            {
                Id = studentId,
                UserId = userId,
                StudentNumber = "STU-2024-0001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            };

            _studentRepositoryMock
                .Setup(x => x.GetStudentWithDetailsAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingStudent);

            var handler = new UpdateStudentCommandHandler(
                _studentRepositoryMock.Object,
                _userManagerMock.Object,
                _auditServiceMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FirstName.Should().Be(command.FirstName);
            result.LastName.Should().Be(command.LastName);

            _studentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync("Update", "Student", It.IsAny<string>()), Times.Once);
        }
    }
}

