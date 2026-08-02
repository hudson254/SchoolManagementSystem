using System;
using System.Collections.Generic;
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
    public class CreateStudentCommandTests
    {
        private readonly CreateStudentCommandValidator _validator;
        private readonly Mock<IUserManagerService> _userManagerMock;
        private readonly Mock<IStudentRepository> _studentRepositoryMock;
        private readonly Mock<SMS.Multitenancy.Interfaces.ITenantContext> _tenantContextMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public CreateStudentCommandTests()
        {
            _validator = new CreateStudentCommandValidator();
            _userManagerMock = new Mock<IUserManagerService>();
            _studentRepositoryMock = new Mock<IStudentRepository>();
            _tenantContextMock = new Mock<SMS.Multitenancy.Interfaces.ITenantContext>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new CreateStudentCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1),
                Password = "Test123!@#"
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
            var command = new CreateStudentCommand
            {
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                PhoneNumber = "",
                DateOfBirth = DateTime.UtcNow.AddDays(1), // Future date to trigger validation
                Password = "abc" // Non-empty but too short to trigger MinimumLength(8)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
            result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
        }

        [Fact]
        public async Task Handle_WithExistingEmail_ShouldThrowConflictException()
        {
            // Arrange
            var command = new CreateStudentCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "existing@example.com",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1),
                Password = "Test123!@#"
            };

            var existingUser = new User();
            _userManagerMock
                .Setup(x => x.GetUserByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            var handler = new CreateStudentCommandHandler(
                _studentRepositoryMock.Object,
                _userManagerMock.Object,
                _tenantContextMock.Object,
                _auditServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateStudent()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var command = new CreateStudentCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "+254712345678",
                DateOfBirth = new DateTime(2000, 1, 1),
                Password = "Test123!@#"
            };

            _tenantContextMock
                .Setup(x => x.TenantId)
                .Returns(Guid.NewGuid().ToString());

            _userManagerMock
                .Setup(x => x.GetUserByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            _userManagerMock
                .Setup(x => x.CreateUserAsync(It.IsAny<string>(), command.Email, command.Password, "Student"))
                .ReturnsAsync((string username, string email, string password, string role) => new User
                {
                    Id = userId,
                    Email = email,
                    UserName = username
                });

            _studentRepositoryMock
                            .Setup(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((Student s, CancellationToken ct) => s);

            var handler = new CreateStudentCommandHandler(
                _studentRepositoryMock.Object,
                _userManagerMock.Object,
                _tenantContextMock.Object,
                _auditServiceMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FirstName.Should().Be(command.FirstName);
            result.LastName.Should().Be(command.LastName);
            result.Email.Should().Be(command.Email);

            _studentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync("Create", "Student", It.IsAny<string>()), Times.Once);
        }
    }
}

