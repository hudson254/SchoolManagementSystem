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
    public class LoginCommandTests
    {
        private readonly LoginCommandValidator _validator;
        private readonly Mock<IUserManagerService> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILoginHistoryRepository> _loginHistoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        public LoginCommandTests()
        {
            _validator = new LoginCommandValidator();
            _userManagerMock = new Mock<IUserManagerService>();
            _jwtServiceMock = new Mock<IJwtService>();
            _auditServiceMock = new Mock<IAuditService>();
            _loginHistoryMock = new Mock<ILoginHistoryRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
        }

        private LoginCommandHandler CreateHandler()
        {
            return new LoginCommandHandler(
                _userManagerMock.Object,
                _jwtServiceMock.Object,
                _auditServiceMock.Object,
                _loginHistoryMock.Object,
                _unitOfWorkMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<LoginCommandHandler>>());
        }

        [Fact]
        public void ValidCommand_ShouldNotHaveValidationErrors()
        {
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!",
                RememberMe = true
            };

            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidCommand_ShouldHaveValidationErrors()
        {
            var command = new LoginCommand
            {
                Email = "invalid-email",
                Password = "",
                RememberMe = true
            };

            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public async Task Handle_WithNonExistentUser_ShouldThrowUnauthorizedException()
        {
            var command = new LoginCommand
            {
                Email = "nonexistent@example.com",
                Password = "Test123!",
                RememberMe = true
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);

            var handler = CreateHandler();

            await Assert.ThrowsAsync<UnauthorizedException>(
                () => handler.Handle(command, CancellationToken.None));

            // RISK-27: failed login (user not found) is recorded
            _loginHistoryMock.Verify(
                x => x.AddAsync(It.Is<LoginHistory>(h => !h.IsSuccessful && h.FailureReason == "User not found"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidPassword_ShouldThrowUnauthorizedException()
        {
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "WrongPassword!",
                RememberMe = true
            };

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                IsEmailVerified = true
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, command.Password))
                .ReturnsAsync(false);

            var handler = CreateHandler();

            await Assert.ThrowsAsync<UnauthorizedException>(
                () => handler.Handle(command, CancellationToken.None));

            // RISK-27: failed login (invalid password) is recorded with the user id
            _loginHistoryMock.Verify(
                x => x.AddAsync(It.Is<LoginHistory>(h => !h.IsSuccessful && h.FailureReason == "Invalid password" && h.UserId == user.Id), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithInactiveAccount_ShouldThrowUnauthorizedException()
        {
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!",
                RememberMe = true
            };

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = false,
                IsEmailVerified = true
            };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, command.Password))
                .ReturnsAsync(true);

            var handler = CreateHandler();

            await Assert.ThrowsAsync<UnauthorizedException>(
                () => handler.Handle(command, CancellationToken.None));

            // RISK-27: failed login (account locked) is recorded
            _loginHistoryMock.Verify(
                x => x.AddAsync(It.Is<LoginHistory>(h => !h.IsSuccessful && h.FailureReason == "Account locked"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidCredentials_ShouldReturnAuthResponse()
        {
            var userId = Guid.NewGuid().ToString();
            var command = new LoginCommand
            {
                Email = "test@example.com",
                Password = "Test123!",
                RememberMe = true
            };

            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                IsEmailVerified = true,
                UserName = "testuser"
            };

            var roles = new List<string> { "Student" };

            _userManagerMock
                .Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(x => x.CheckPasswordAsync(user, command.Password))
                .ReturnsAsync(true);

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns("test-access-token");

            _userManagerMock
                .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("test-refresh-token");

            var handler = CreateHandler();

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test-access-token");
            result.RefreshToken.Should().Be("test-refresh-token");
            result.UserId.Should().Be(userId);
            result.Email.Should().Be(command.Email);
            result.Roles.Should().Contain("Student");

            _auditServiceMock.Verify(x => x.LogAsync("Login", userId, "User logged in successfully"), Times.Once);

            // RISK-27: successful login is persisted
            _loginHistoryMock.Verify(
                x => x.AddAsync(It.Is<LoginHistory>(h => h.IsSuccessful && h.UserId == userId), It.IsAny<CancellationToken>()),
                Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}