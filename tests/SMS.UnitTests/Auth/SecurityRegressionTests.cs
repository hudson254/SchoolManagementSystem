using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SMS.Application.Exceptions;
using SMS.Application.Features.Auth.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Identity.Models;
using SMS.Identity.Services;
using Xunit;

namespace SMS.UnitTests.Auth
{
    /// <summary>
    /// Security regression tests for RISK-02 (refresh token bypass) and
    /// RISK-03 (hardcoded Student claim in all JWTs). These tests would have
    /// caught the original vulnerabilities before they reached production.
    /// </summary>
    public class SecurityRegressionTests
    {
        private static readonly string TestSecret =
            "test-jwt-secret-key-that-is-at-least-64-characters-long-for-testing-purposes-only";

        private static JwtSettings TestSettings => new()
        {
            Secret = TestSecret,
            Issuer = "SMSAPI",
            Audience = "SMSWeb",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        private static JwtService CreateJwtService() =>
            new(Options.Create(TestSettings), NullLogger<JwtService>.Instance);

        private static SymmetricSecurityKey SigningKey => new(System.Text.Encoding.UTF8.GetBytes(TestSecret));

        /// <summary>
        /// RISK-03 regression: A JWT issued for a Lecturer must never contain
        /// a "Student" role claim. Previously JwtService injected a hardcoded
        /// "Student" claim into every token regardless of the user's actual
        /// roles, granting unintended student-level access to lecturers and
        /// administrators.
        /// </summary>
        [Fact]
        public void Jwt_ForLecturer_ShouldNeverContainStudentClaim()
        {
            // Arrange
            var jwt = CreateJwtService();
            var userId = Guid.NewGuid().ToString();
            var roles = new[] { "Lecturer" };

            // Act
            var token = jwt.GenerateAccessToken(userId, "lecturer@school.edu", roles);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();

            roleClaims.Should().Contain("Lecturer");
            roleClaims.Should().NotContain("Student", "a Lecturer token must not carry a Student role claim");
        }

        /// <summary>
        /// RISK-03 regression: A JWT issued for an Administrator must only
        /// contain the Administrator role, never the hardcoded Student role.
        /// </summary>
        [Fact]
        public void Jwt_ForAdministrator_ShouldOnlyContainAdministratorRole()
        {
            // Arrange
            var jwt = CreateJwtService();
            var userId = Guid.NewGuid().ToString();
            var roles = new[] { "Administrator" };

            // Act
            var token = jwt.GenerateAccessToken(userId, "admin@school.edu", roles);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();

            roleClaims.Should().ContainSingle().Which.Should().Be("Administrator");
            roleClaims.Should().NotContain("Student");
        }

        /// <summary>
        /// RISK-03 regression: A JWT issued for a user with no roles must not
        /// contain any role claim at all (previously it always had "Student").
        /// </summary>
        [Fact]
        public void Jwt_ForUserWithNoRoles_ShouldContainNoRoleClaims()
        {
            // Arrange
            var jwt = CreateJwtService();
            var userId = Guid.NewGuid().ToString();
            var roles = Array.Empty<string>();

            // Act
            var token = jwt.GenerateAccessToken(userId, "noroles@school.edu", roles);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();

            roleClaims.Should().BeEmpty("a user with no assigned roles must not receive any role claim");
        }

        /// <summary>
        /// RISK-02 regression: A refresh request with a forged refresh token
        /// (valid base64 shape but not matching the stored token) must be
        /// rejected. Previously the handler only checked base64 length.
        /// </summary>
        [Fact]
        public async Task Refresh_WithForgedRefreshToken_ShouldBeRejected()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var storedRefreshToken = "stored-valid-refresh-token-value";
            var forgedRefreshToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

            // Build an expired access token carrying the user id so the
            // handler can extract it.
            var jwt = CreateJwtService();
            var accessToken = jwt.GenerateAccessToken(userId, "user@school.edu", new[] { "Student" });

            var user = new User { Id = userId, Email = "user@school.edu", IsActive = true };

            var userManagerMock = new Mock<IUserManagerService>();
            userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            // The stored token does NOT match the forged one -> validation fails.
            userManagerMock.Setup(x => x.ValidateRefreshTokenAsync(userId, forgedRefreshToken)).ReturnsAsync(false);

            var handler = new RefreshTokenCommandHandler(jwt, userManagerMock.Object);

            var command = new RefreshTokenCommand
            {
                AccessToken = accessToken,
                RefreshToken = forgedRefreshToken
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
            userManagerMock.Verify(x => x.ValidateRefreshTokenAsync(userId, forgedRefreshToken), Times.Once);
            // Must NOT issue a new refresh token when validation fails.
            userManagerMock.Verify(x => x.GenerateRefreshTokenAsync(userId), Times.Never);
        }

        /// <summary>
        /// RISK-02 regression: A refresh request with an expired stored refresh
        /// token must be rejected. The UserManagerService reports the stored
        /// token as expired (validation returns false).
        /// </summary>
        [Fact]
        public async Task Refresh_WithExpiredStoredRefreshToken_ShouldBeRejected()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var expiredRefreshToken = "expired-but-shape-valid-token";

            var jwt = CreateJwtService();
            var accessToken = jwt.GenerateAccessToken(userId, "user@school.edu", new[] { "Student" });

            var user = new User { Id = userId, Email = "user@school.edu", IsActive = true };

            var userManagerMock = new Mock<IUserManagerService>();
            userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            // Stored token exists but its expiry has passed -> false.
            userManagerMock.Setup(x => x.ValidateRefreshTokenAsync(userId, expiredRefreshToken)).ReturnsAsync(false);

            var handler = new RefreshTokenCommandHandler(jwt, userManagerMock.Object);

            var command = new RefreshTokenCommand
            {
                AccessToken = accessToken,
                RefreshToken = expiredRefreshToken
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
            userManagerMock.Verify(x => x.GenerateRefreshTokenAsync(userId), Times.Never);
        }

        /// <summary>
        /// RISK-02 positive: A refresh request with a valid, non-expired stored
        /// refresh token must succeed AND rotate the token (issue a new one).
        /// </summary>
        [Fact]
        public async Task Refresh_WithValidStoredRefreshToken_ShouldSucceedAndRotate()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var validRefreshToken = "valid-stored-refresh-token";
            var rotatedRefreshToken = "new-rotated-refresh-token";

            var jwt = CreateJwtService();
            var accessToken = jwt.GenerateAccessToken(userId, "user@school.edu", new[] { "Student" });

            var user = new User { Id = userId, Email = "user@school.edu", IsActive = true, FirstName = "Test", LastName = "User" };

            var userManagerMock = new Mock<IUserManagerService>();
            userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            userManagerMock.Setup(x => x.ValidateRefreshTokenAsync(userId, validRefreshToken)).ReturnsAsync(true);
            userManagerMock.Setup(x => x.GetUserRolesAsync(userId)).ReturnsAsync(new[] { "Student" });
            userManagerMock.Setup(x => x.GenerateRefreshTokenAsync(userId)).ReturnsAsync(rotatedRefreshToken);

            var handler = new RefreshTokenCommandHandler(jwt, userManagerMock.Object);

            var command = new RefreshTokenCommand
            {
                AccessToken = accessToken,
                RefreshToken = validRefreshToken
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.RefreshToken.Should().Be(rotatedRefreshToken, "the refresh token must be rotated on every successful refresh");
            result.AccessToken.Should().NotBeNullOrEmpty();
            userManagerMock.Verify(x => x.ValidateRefreshTokenAsync(userId, validRefreshToken), Times.Once);
            userManagerMock.Verify(x => x.GenerateRefreshTokenAsync(userId), Times.Once);
        }

        /// <summary>
        /// RISK-02 regression: A refresh request for an inactive user must be
        /// rejected even if the refresh token is otherwise valid.
        /// </summary>
        [Fact]
        public async Task Refresh_ForInactiveUser_ShouldBeRejected()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var jwt = CreateJwtService();
            var accessToken = jwt.GenerateAccessToken(userId, "inactive@school.edu", new[] { "Student" });

            var inactiveUser = new User { Id = userId, Email = "inactive@school.edu", IsActive = false };

            var userManagerMock = new Mock<IUserManagerService>();
            userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(inactiveUser);

            var handler = new RefreshTokenCommandHandler(jwt, userManagerMock.Object);

            var command = new RefreshTokenCommand
            {
                AccessToken = accessToken,
                RefreshToken = "any-token"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
            userManagerMock.Verify(x => x.ValidateRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// RISK-05 regression: Logout must actually revoke the refresh token
        /// and add the access token's jti to the deny-list. Previously the
        /// LogoutCommandHandler was an empty stub that did nothing, so a
        /// stolen token remained valid until natural expiry.
        /// </summary>
        [Fact]
        public async Task Logout_ShouldRevokeRefreshTokenAndDenyListAccessToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var jti = Guid.NewGuid().ToString();

            var userManagerMock = new Mock<IUserManagerService>();
            userManagerMock.Setup(x => x.RevokeRefreshTokenAsync(userId.ToString())).ReturnsAsync(true);

            var tokenRevocationMock = new Mock<ITokenRevocationService>();
            var auditMock = new Mock<IAuditService>();

            var handler = new LogoutCommandHandler(
                userManagerMock.Object,
                tokenRevocationMock.Object,
                auditMock.Object,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<LogoutCommandHandler>.Instance);

            var command = new LogoutCommand { UserId = userId, AccessTokenJti = jti };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            userManagerMock.Verify(x => x.RevokeRefreshTokenAsync(userId.ToString()), Times.Once);
            tokenRevocationMock.Verify(x => x.RevokeAccessTokenAsync(jti), Times.Once);
            auditMock.Verify(x => x.LogAsync("Logout", userId.ToString(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// RISK-05 regression: After logout, a refresh attempt using the
        /// revoked refresh token must be rejected. The UserManagerService
        /// reports the stored token as revoked (validation returns false).
        /// </summary>
        [Fact]
        public async Task Refresh_AfterLogout_ShouldBeRejected()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var revokedRefreshToken = "now-revoked-refresh-token";

            var jwt = CreateJwtService();
            var accessToken = jwt.GenerateAccessToken(userId, "user@school.edu", new[] { "Student" });

            var user = new User { Id = userId, Email = "user@school.edu", IsActive = true };

            var userManagerMock = new Mock<IUserManagerService>();
            userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            // After logout, RevokeRefreshTokenAsync nulls out the stored token,
            // so ValidateRefreshTokenAsync returns false.
            userManagerMock.Setup(x => x.ValidateRefreshTokenAsync(userId, revokedRefreshToken)).ReturnsAsync(false);

            var handler = new RefreshTokenCommandHandler(jwt, userManagerMock.Object);

            var command = new RefreshTokenCommand
            {
                AccessToken = accessToken,
                RefreshToken = revokedRefreshToken
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
            userManagerMock.Verify(x => x.GenerateRefreshTokenAsync(userId), Times.Never);
        }
    }
}
