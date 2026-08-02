using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using SMS.Application.Exceptions;
using SMS.Application.Features.Auth.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.Auth
{
    public class RegisterCommandTests
    {
        private readonly RegisterCommandValidator _validator;
        private readonly Mock<IUserManagerService> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public RegisterCommandTests()
        {
            _validator = new RegisterCommandValidator();
            _userManagerMock = new Mock<IUserManagerService>();
            _jwtServiceMock = new Mock<IJwtService>();
            _auditServiceMock = new Mock<IAuditService>();
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#",
                Role = "Student"
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
            var command = new RegisterCommand
            {
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                Password = "weak",
                ConfirmPassword = "different",
                Role = "InvalidRole"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
            result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
        }

        [Fact]
        public async Task Handle_WithExistingEmail_ShouldThrowConflictException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "existing@example.com",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#",
                Role = "Student"
            };

            var existingUser = new User { Email = "existing@example.com" };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            var handler = new RegisterCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _auditServiceMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler>>());

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldRegisterUser()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#",
                Role = "Student"
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            _userManagerMock
                .Setup(x => x.CreateUserAsync(command.Email, command.Email, command.Password, command.Role))
                .ReturnsAsync((string email, string username, string password, string role) => new User
                {
                    Id = userId,
                    Email = email,
                    UserName = username,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    IsActive = true
                });

            _userManagerMock
                .Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { command.Role });

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");

            _userManagerMock
                .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");

            var handler = new RegisterCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _auditServiceMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler>>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test-access-token");
            result.RefreshToken.Should().Be("test-refresh-token");
            result.Email.Should().Be(command.Email);
            result.Roles.Should().Contain(command.Role);

            _userManagerMock.Verify(x => x.CreateUserAsync(command.Email, command.Email, command.Password, command.Role), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync("Register", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithPasswordMismatch_ShouldThrowValidationException()
        {
            // Arrange
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#",
                ConfirmPassword = "DifferentPassword!",
                Role = "Student"
            };

            var handler = new RegisterCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _auditServiceMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<RegisterCommandHandler>>());

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => handler.Handle(command, CancellationToken.None));
        }
    }
}

