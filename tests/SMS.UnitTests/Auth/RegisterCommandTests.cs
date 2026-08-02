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
using SMS.Identity.Services;
using Xunit;

namespace SMS.UnitTests.Auth
{
    public class RegisterCommandTests
    {
        private readonly RegisterCommandValidator _validator;
        private readonly Mock<IUserManagerService> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;

        public RegisterCommandTests()
        {
            _validator = new RegisterCommandValidator();
            _userManagerMock = new Mock<IUserManagerService>();
            _jwtServiceMock = new Mock<IJwtService>();
            _emailServiceMock = new Mock<IEmailService>();
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
                PhoneNumber = "+254712345678",
                Organization = "Test School",
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
                PhoneNumber = "",
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
            result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
            result.ShouldHaveValidationErrorFor(x => x.Role);
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
                PhoneNumber = "+254712345678",
                Role = "Student"
            };

            var existingUser = new User { Email = "existing@example.com" };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            var handler = new RegisterCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _emailServiceMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<RegisterCommandHandler>>());

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldRegisterUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var command = new RegisterCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#",
                PhoneNumber = "+254712345678",
                Organization = "Test School",
                Role = "Student"
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            _userManagerMock
                .Setup(x => x.CreateUserAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync((User user, string password) => new IdentityResult { Succeeded = true });

            _userManagerMock
                .Setup(x => x.AddToRoleAsync(It.IsAny<User>(), command.Role))
                .Returns(Task.CompletedTask);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>()))
                .Returns("test-access-token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("test-refresh-token");

            _userManagerMock
                .Setup(x => x.GenerateEmailVerificationTokenAsync(It.IsAny<User>()))
                .ReturnsAsync("test-verification-token");

            var handler = new RegisterCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _emailServiceMock.Object,
                _auditServiceMock.Object,
                Mock.Of<ILogger<RegisterCommandHandler>>());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test-access-token");
            result.RefreshToken.Should().Be("test-refresh-token");
            result.Email.Should().Be(command.Email);
            result.FirstName.Should().Be(command.FirstName);
            result.LastName.Should().Be(command.LastName);
            result.Roles.Should().Contain(command.Role);
            result.RequiresEmailVerification.Should().BeTrue();

            _userManagerMock.Verify(x => x.CreateUserAsync(It.IsAny<User>(), command.Password), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), command.Role), Times.Once);
            _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
            _auditServiceMock.Verify(x => x.LogAsync("User", "Register", It.IsAny<Guid>(), null, It.IsAny<string>()), Times.Once);
        }
    }
}