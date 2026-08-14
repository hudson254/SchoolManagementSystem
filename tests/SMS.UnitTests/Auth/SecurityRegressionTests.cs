using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
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
    /// Expanded to include comprehensive JWT algorithm security tests.
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

        private static SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(TestSecret));

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
            userManagerMock.Setup(x => x.RotateRefreshTokenAsync(userId, validRefreshToken)).ReturnsAsync(rotatedRefreshToken);

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
            userManagerMock.Verify(x => x.RotateRefreshTokenAsync(userId, validRefreshToken), Times.Once);
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
        /// and add the access token's jti to the deny-list.
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
        /// revoked refresh token must be rejected.
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

        /// <summary>
        /// JWT must reject tokens with algorithm "none" (unsigned JWTs).
        /// The ValidAlgorithms validation in JwtService.ValidateToken
        /// should reject unsigned tokens that have no signature.
        /// </summary>
        [Fact]
        public void ValidateToken_WithAlgNone_ShouldReturnFalse()
        {
            // Arrange - Create an unsigned JWT with alg: none
            var jwt = CreateJwtService();
            var unsignedToken = CreateUnsignedJwt("test-user", "test@school.edu", new[] { "Student" });

            // Act
            var result = jwt.ValidateToken(unsignedToken);

            // Assert - must be rejected
            result.Should().BeFalse("unsigned tokens with alg:none must be rejected");
        }

        /// <summary>
        /// JWT must reject tokens with a wrong algorithm (HS512 instead of HS256).
        /// Although both use HMAC, the algorithm must match exactly.
        /// </summary>
        [Fact]
        public void ValidateToken_WithWrongAlgorithm_ShouldReturnFalse()
        {
            // Arrange - Create a token signed with HS512 instead of HS256
            var jwt = CreateJwtService();
            var wrongAlgToken = CreateJwtWithAlgorithm("HS512", TestSecret, "test-user", new[] { "Student" });

            // Act
            var result = jwt.ValidateToken(wrongAlgToken);

            // Assert - must be rejected due to algorithm mismatch
            result.Should().BeFalse("tokens signed with a different algorithm than HS256 must be rejected");
        }

        /// <summary>
        /// JWT must reject tokens with forged signatures (modified payload).
        /// </summary>
        [Fact]
        public void ValidateToken_WithForgedSignature_ShouldReturnFalse()
        {
            // Arrange - Create a valid token, then modify its payload
            var jwt = CreateJwtService();
            var validToken = jwt.GenerateAccessToken("test-user", "test@school.edu", new[] { "Student" });
            var forgedToken = ForgeJwtPayload(validToken, "different-user");

            // Act
            var result = jwt.ValidateToken(forgedToken);

            // Assert - must be rejected
            result.Should().BeFalse("tokens with forged payloads must be rejected");
        }

        /// <summary>
        /// JWT must reject tokens with modified claims (role escalation).
        /// </summary>
        [Fact]
        public void ValidateToken_WithRoleEscalation_ShouldReturnFalse()
        {
            // Arrange - Create a valid token, then modify its role claims
            var jwt = CreateJwtService();
            var validToken = jwt.GenerateAccessToken("test-user", "test@school.edu", new[] { "Student" });
            var escalatedToken = ForgeJwtPayload(validToken, "test-user", new[] { "Administrator" });

            // Act
            var result = jwt.ValidateToken(escalatedToken);

            // Assert - must be rejected
            result.Should().BeFalse("tokens with modified role claims must be rejected");
        }

        /// <summary>
        /// JWT must reject tokens with an invalid issuer.
        /// </summary>
        [Fact]
        public void ValidateToken_WithInvalidIssuer_ShouldReturnFalse()
        {
            // Arrange - Create a token with a different issuer
            var jwt = CreateJwtService();
            var token = CreateJwtWithIssuer("WrongIssuer", TestSecret, "test-user", new[] { "Student" });

            // Act
            var result = jwt.ValidateToken(token);

            // Assert - must be rejected
            result.Should().BeFalse("tokens with an invalid issuer must be rejected");
        }

        /// <summary>
        /// JWT must reject tokens with an invalid audience.
        /// </summary>
        [Fact]
        public void ValidateToken_WithInvalidAudience_ShouldReturnFalse()
        {
            // Arrange - Create a token with a different audience
            var jwt = CreateJwtService();
            var token = CreateJwtWithAudience("WrongAudience", TestSecret, "test-user", new[] { "Student" });

            // Act
            var result = jwt.ValidateToken(token);

            // Assert - must be rejected
            result.Should().BeFalse("tokens with an invalid audience must be rejected");
        }

        /// <summary>
        /// JWT must reject expired tokens.
        /// </summary>
        [Fact]
        public void ValidateToken_ExpiredToken_ShouldReturnFalse()
        {
            // Arrange - Create an expired token
            var jwt = CreateJwtService();
            var expiredToken = CreateExpiredJwt(TestSecret, "test-user", new[] { "Student" });

            // Act
            var result = jwt.ValidateToken(expiredToken);

            // Assert - must be rejected
            result.Should().BeFalse("expired tokens must be rejected");
        }

        /// <summary>
        /// JWT must accept a valid, properly signed token with correct claims.
        /// </summary>
        [Fact]
        public void ValidateToken_ValidToken_ShouldReturnTrue()
        {
            // Arrange
            var jwt = CreateJwtService();
            var token = jwt.GenerateAccessToken("test-user", "test@school.edu", new[] { "Student" });

            // Act
            var result = jwt.ValidateToken(token);

            // Assert
            result.Should().BeTrue("a valid token with correct signature and claims must be accepted");
        }

        /// <summary>
        /// JWT must contain the nbf (not before) and iat (issued at) claims.
        /// </summary>
        [Fact]
        public void Jwt_ShouldContainNbfAndIatClaims()
        {
            // Arrange
            var jwt = CreateJwtService();
            var token = jwt.GenerateAccessToken("test-user", "test@school.edu", new[] { "Student" });

            // Act
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Nbf, "JWT must contain nbf claim");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat, "JWT must contain iat claim");
        }

        /// <summary>
        /// JWT must contain a jti (JWT ID) claim for revocation tracking.
        /// </summary>
        [Fact]
        public void Jwt_ShouldContainJtiClaim()
        {
            // Arrange
            var jwt = CreateJwtService();
            var token = jwt.GenerateAccessToken("test-user", "test@school.edu", new[] { "Student" });

            // Act
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti, "JWT must contain a unique jti claim");
        }

        /// <summary>
        /// GetPrincipalFromExpiredToken must reject tokens with algorithm "none".
        /// The actual exception type is SecurityTokenInvalidSignatureException
        /// (a subclass of SecurityTokenException).
        /// </summary>
        [Fact]
        public void GetPrincipalFromExpiredToken_WithAlgNone_ShouldThrow()
        {
            // Arrange
            var jwt = CreateJwtService();
            var unsignedToken = CreateUnsignedJwt("test-user", "test@school.edu", new[] { "Student" });

            // Act & Assert
            // SecurityTokenInvalidSignatureException is a subclass of SecurityTokenException
            Assert.Throws<SecurityTokenInvalidSignatureException>(() => jwt.GetPrincipalFromExpiredToken(unsignedToken));
        }

        /// <summary>
        /// GetPrincipalFromExpiredToken must reject tokens with wrong algorithm.
        /// The actual exception type is SecurityTokenSignatureKeyNotFoundException
        /// (a subclass of SecurityTokenException).
        /// </summary>
        [Fact]
        public void GetPrincipalFromExpiredToken_WithWrongAlgorithm_ShouldThrow()
        {
            // Arrange
            var jwt = CreateJwtService();
            var wrongAlgToken = CreateJwtWithAlgorithm("HS512", TestSecret, "test-user", new[] { "Student" });

            // Act & Assert
            // SecurityTokenSignatureKeyNotFoundException is a subclass of SecurityTokenException
            Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() => jwt.GetPrincipalFromExpiredToken(wrongAlgToken));
        }

        /// <summary>
        /// GetPrincipalFromExpiredToken must accept a valid expired token.
        /// </summary>
        [Fact]
        public void GetPrincipalFromExpiredToken_ValidExpiredToken_ShouldSucceed()
        {
            // Arrange
            var jwt = CreateJwtService();
            var expiredToken = CreateExpiredJwt(TestSecret, "test-user", new[] { "Student" });

            // Act
            var principal = jwt.GetPrincipalFromExpiredToken(expiredToken);

            // Assert
            principal.Should().NotBeNull();
            principal.FindFirst("sub")?.Value.Should().Be("test-user");
        }

        // ==================== Helper Methods ====================

        private static string CreateUnsignedJwt(string userId, string username, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var role in roles)
                claims.Add(new Claim("role", role));

            // Create token with alg: none (no signing key)
            var token = new JwtSecurityToken(
                issuer: "SMSAPI",
                audience: "SMSWeb",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: null
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string CreateJwtWithAlgorithm(string algorithm, string secret, string userId, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Name, "test@school.edu"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var role in roles)
                claims.Add(new Claim("role", role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = algorithm switch
            {
                "HS256" => new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                "HS384" => new SigningCredentials(key, SecurityAlgorithms.HmacSha384),
                "HS512" => new SigningCredentials(key, SecurityAlgorithms.HmacSha512),
                _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
            };

            var token = new JwtSecurityToken(
                issuer: "SMSAPI",
                audience: "SMSWeb",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string CreateJwtWithIssuer(string issuer, string secret, string userId, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Name, "test@school.edu"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var role in roles)
                claims.Add(new Claim("role", role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: "SMSWeb",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string CreateJwtWithAudience(string audience, string secret, string userId, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Name, "test@school.edu"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var role in roles)
                claims.Add(new Claim("role", role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "SMSAPI",
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string CreateExpiredJwt(string secret, string userId, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Name, "test@school.edu"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var role in roles)
                claims.Add(new Claim("role", role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "SMSAPI",
                audience: "SMSWeb",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(-30), // 30 minutes ago (expired)
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string ForgeJwtPayload(string validToken, string newUserId)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(validToken);

            // Create a new payload with modified user ID but keep the original header and signature
            var claims = jwtToken.Claims.Where(c => c.Type != "sub" && c.Type != ClaimTypes.NameIdentifier).ToList();
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, newUserId));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, newUserId));

            // Rebuild with same header and signature (will be invalid because signature doesn't match new payload)
            var token = new JwtSecurityToken(
                issuer: jwtToken.Issuer,
                audience: jwtToken.Audiences.FirstOrDefault() ?? "SMSWeb",
                claims: claims,
                expires: jwtToken.ValidTo,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("different-secret-key-that-does-not-match-at-all!")),
                    SecurityAlgorithms.HmacSha256)
            );
            return handler.WriteToken(token);
        }

        private static string ForgeJwtPayload(string validToken, string newUserId, IEnumerable<string> newRoles)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(validToken);

            // Remove role claims and add new ones
            var claims = jwtToken.Claims.Where(c => c.Type != "role" && c.Type != "sub" && c.Type != ClaimTypes.NameIdentifier).ToList();
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, newUserId));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, newUserId));
            foreach (var role in newRoles)
                claims.Add(new Claim("role", role));

            var token = new JwtSecurityToken(
                issuer: jwtToken.Issuer,
                audience: jwtToken.Audiences.FirstOrDefault() ?? "SMSWeb",
                claims: claims,
                expires: jwtToken.ValidTo,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("different-secret-key-that-does-not-match-at-all!")),
                    SecurityAlgorithms.HmacSha256)
            );
            return handler.WriteToken(token);
        }
    }
}
