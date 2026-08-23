using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Infrastructure.Services
{
    public class UserManagerService : IUserManagerService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<UserManagerService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SMS.Multitenancy.Interfaces.ITenantContext _tenantContext;

        public UserManagerService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ILogger<UserManagerService> logger,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            SMS.Multitenancy.Interfaces.ITenantContext tenantContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _tenantContext = tenantContext;
        }

        // User Retrieval
        public async Task<User> FindByIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<User> FindByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<User> FindByUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return null;
            return await _userManager.FindByNameAsync(username);
        }

        public async Task<User> FindByRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return null;
            // Search by hash rather than plaintext token
            var tokenHash = ComputeSha256Hash(refreshToken);
            var users = await _userManager.Users
                .Where(u => u.RefreshTokenHash == tokenHash)
                .ToListAsync();
            return users.FirstOrDefault();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            return await _userManager.GetUsersInRoleAsync(role);
        }

        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return false;
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            if (user == null || string.IsNullOrEmpty(password)) return false;
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<User> CreateUserAsync(string username, string email, string password, string role = null)
        {
            var user = new User
            {
                UserName = username,
                Email = email,
                FirstName = username,
                LastName = "",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                RefreshTokenHash = null,
                RefreshToken = null,
                EmailConfirmed = true,
                IsEmailVerified = true
            };

            // Assign the tenant from the current request so the AspNetUsers
            // row satisfies the FK_AspNetUsers_Tenants_TenantId constraint.
            // The User entity is excluded from the global tenant filter and
            // is not auto-assigned by ApplicationDbContext.SaveChangesAsync
            // (only ITenantAwareEntity entities are), so we set it explicitly.
            // A missing/invalid tenant is a controlled application error:
            // we fail BEFORE Identity issues any INSERT, so the database FK
            // constraint is never reached with Guid.Empty.
            user.TenantId = await ResolveTenantIdForUserAsync(username);

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user: {Errors}", errorMessages);
                return null;
            }

            if (!string.IsNullOrEmpty(role))
                await AddToRoleAsync(user, role);

            return user;
        }

        /// <summary>
        /// Resolves and validates the tenant ID for a new Identity user.
        /// The tenant is read from the current HTTP request (set by
        /// TenantResolutionMiddleware) with a fallback to the injected
        /// ITenantContext. The value must be a present, non-empty GUID that
        /// references an active tenant; otherwise a controlled
        /// <see cref="ValidationException"/> is thrown BEFORE any database write.
        /// </summary>
        private async Task<Guid> ResolveTenantIdForUserAsync(string username)
        {
            Guid tenantId = Guid.Empty;

            // Primary source: TenantResolutionMiddleware stores the resolved
            // tenant GUID in HttpContext.Items["TenantId"] before controllers run.
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext != null &&
                httpContext.Items.TryGetValue("TenantId", out var tenantIdObj))
            {
                if (tenantIdObj is Guid tenantGuid)
                    tenantId = tenantGuid;
                else if (tenantIdObj is string tenantIdString)
                    Guid.TryParse(tenantIdString, out tenantId);
            }

            // Fallback: the constructor-injected tenant context may already
            // carry the tenant (e.g. resolved before request items are set).
            if (tenantId == Guid.Empty &&
                _tenantContext != null &&
                Guid.TryParse(_tenantContext.TenantId, out var contextTenantId))
            {
                tenantId = contextTenantId;
            }

            if (tenantId == Guid.Empty)
            {
                _logger.LogError("CreateUserAsync: TenantId could not be resolved for user {Username}.", username);
                throw new ValidationException(
                    "A valid tenant context is required to create a user. Provide an X-Tenant-Id header that matches an active tenant.");
            }

            // Defensive: verify the tenant exists and is active. For normal
            // HTTP requests TenantResolutionMiddleware already guarantees this,
            // but this also covers any non-HTTP call path that reaches here.
            var tenantExists = await _context.Tenants
                .AnyAsync(t => t.Id == tenantId && t.IsActive && !t.IsDeleted);
            if (!tenantExists)
            {
                _logger.LogError("CreateUserAsync: Tenant {TenantId} does not exist or is inactive for user {Username}.", tenantId, username);
                throw new ValidationException(
                    "The tenant context is invalid or the tenant is inactive.");
            }

            return tenantId;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            if (user == null) return false;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUserAsync(string userId, object userData)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return await UpdateUserAsync(user);
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> LockUserAsync(string userId, string reason = null)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            return (await _userManager.UpdateAsync(user)).Succeeded;
        }

        public async Task<bool> UnlockUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.IsActive = true;
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            return (await _userManager.UpdateAsync(user)).Succeeded;
        }

        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return (await _userManager.ChangePasswordAsync(user, currentPassword, newPassword)).Succeeded;
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return !string.IsNullOrEmpty(token);
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword)) return false;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            return (await _userManager.ResetPasswordAsync(user, token, newPassword)).Succeeded;
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            return await ForgotPasswordAsync(email);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(User user)
        {
            if (user == null) return null;
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<bool> VerifyEmailAsync(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return (await _userManager.ConfirmEmailAsync(user, token)).Succeeded;
        }

        public async Task<string> GenerateEmailVerificationTokenAsync(User user)
        {
            if (user == null) return null;
            return await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task<bool> ConfirmEmailAsync(User user, string token)
        {
            if (user == null || string.IsNullOrEmpty(token)) return false;
            return (await _userManager.ConfirmEmailAsync(user, token)).Succeeded;
        }

        public async Task<bool> AssignRoleAsync(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return await AddToRoleAsync(user, role);
        }

        public async Task<bool> AddToRoleAsync(User user, string role)
        {
            if (user == null || string.IsNullOrEmpty(role)) return false;
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new Role { Name = role, NormalizedName = role.ToUpper() });
            return (await _userManager.AddToRoleAsync(user, role)).Succeeded;
        }

        public async Task<bool> RemoveRoleAsync(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return (await _userManager.RemoveFromRoleAsync(user, role)).Succeeded;
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return Enumerable.Empty<string>();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Enumerable.Empty<string>();
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IEnumerable<string>> GetRolesAsync(User user)
        {
            if (user == null) return Enumerable.Empty<string>();
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IEnumerable<string>> GetPermissionsAsync(User user)
        {
            if (user == null) return Enumerable.Empty<string>();
            var roles = await _userManager.GetRolesAsync(user);
            if (roles == null || !roles.Any()) return Enumerable.Empty<string>();

            var permissions = new List<string>();
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;
                var rolePermissions = await _context.Set<RolePermission>()
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == role.Id && rp.IsGranted)
                    .Select(rp => $"{rp.Resource}.{rp.PermissionType}")
                    .ToListAsync();
                permissions.AddRange(rolePermissions);
            }
            return permissions.Distinct().ToList();
        }

        public async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            // Generate a cryptographically secure 64-byte random token
            var randomNumber = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);

            // Hash the token for storage (never store plaintext)
            var tokenHash = ComputeSha256Hash(refreshToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Store only the hash, never the plaintext token
                user.RefreshTokenHash = tokenHash;
                // Clear the legacy plaintext field
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                // Assign a family ID if not already set (for reuse detection)
                if (user.RefreshTokenFamilyId == null)
                {
                    user.RefreshTokenFamilyId = Guid.NewGuid();
                }

                await _userManager.UpdateAsync(user);
            }
            return refreshToken;
        }

        /// <summary>
        /// Computes SHA-256 hash of the input string.
        /// </summary>
        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
            var builder = new System.Text.StringBuilder();
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(refreshToken)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Compare against the stored hash, never the plaintext token
            var tokenHash = ComputeSha256Hash(refreshToken);
            return string.Equals(user.RefreshTokenHash, tokenHash, StringComparison.OrdinalIgnoreCase)
                   && user.RefreshTokenExpiryTime > DateTime.UtcNow;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.RefreshToken = null;
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiryTime = null;
            return (await _userManager.UpdateAsync(user)).Succeeded;
        }

        public async Task<bool> IsRefreshTokenReusedAsync(string userId, string refreshToken)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(refreshToken)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.RefreshTokenFamilyId == null) return false;

            // Check if the token hash matches the stored previous token hash.
            // After rotation:
            //   - RefreshTokenHash stores the CURRENT token hash (for validation)
            //   - RefreshToken stores the PREVIOUS token hash (for reuse detection)
            // If the presented token hash matches the PREVIOUS hash, it's a reuse attempt.
            var tokenHash = ComputeSha256Hash(refreshToken);

            // Check against the previous hash stored in RefreshToken field
            // (which stores the old rotation hash, not a plaintext token)
            if (!string.IsNullOrEmpty(user.RefreshToken) &&
                string.Equals(user.RefreshToken, tokenHash, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Also check against the current hash - if the presented token matches the
            // current stored hash, validation will succeed in ValidateRefreshTokenAsync.
            // This is NOT a reuse detection. Only check if the token matches the previous hash.
            return false;
        }

        public async Task<bool> RevokeRefreshTokenFamilyAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Clear all refresh token data, effectively revoking the entire family
            user.RefreshToken = null;
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiryTime = null;
            user.RefreshTokenFamilyId = null;
            return (await _userManager.UpdateAsync(user)).Succeeded;
        }

        public async Task<bool> RevokeAllRefreshTokensAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Clear all refresh token data, effectively revoking all token families
            user.RefreshToken = null;
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiryTime = null;
            user.RefreshTokenFamilyId = null;
            return (await _userManager.UpdateAsync(user)).Succeeded;
        }

        public async Task<string> RotateRefreshTokenAsync(string userId, string currentRefreshToken)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            // Generate a new cryptographically secure token
            var randomNumber = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var newRefreshToken = Convert.ToBase64String(randomNumber);

            // Hash the new token
            var newTokenHash = ComputeSha256Hash(newRefreshToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Store the hash of the old current token for reuse detection
                var oldTokenHash = ComputeSha256Hash(currentRefreshToken);

                user.RefreshTokenHash = newTokenHash; // Store the new token hash for validation
                user.RefreshToken = oldTokenHash; // Store previous hash in RefreshToken field for reuse detection
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                if (user.RefreshTokenFamilyId == null)
                    user.RefreshTokenFamilyId = Guid.NewGuid();

                await _userManager.UpdateAsync(user);
            }
            return newRefreshToken;
        }

        public async Task<bool> LogoutAsync(string userId) => await RevokeRefreshTokenAsync(userId);

        public async Task<bool> IsUserOnlineAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            try
            {
                var recentThreshold = DateTime.UtcNow.AddMinutes(-15);
                return await _context.Set<LoginHistory>()
                    .AsNoTracking()
.AnyAsync(lh => lh.UserId == userId && lh.LoginTime >= recentThreshold && lh.LogoutTime == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking online status for user {UserId}", userId);
                return false;
            }
        }

        public async Task<DateTime?> GetLastLoginTimeAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var user = await _userManager.FindByIdAsync(userId);
            return user?.UpdatedAt;
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            return (await _userManager.FindByIdAsync(userId)) != null;
        }

        public async Task<bool> IsUserActiveAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && user.IsActive && !user.LockoutEnabled;
        }

        public async Task<bool> IsEmailConfirmedAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return await _userManager.IsEmailConfirmedAsync(user);
        }

        // Additional Methods required by interface
        public async Task<User> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> ValidatePasswordResetTokenAsync(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return false;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            return await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token);
        }

        public async Task<bool> ValidateEmailVerificationTokenAsync(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token)) return false;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            return await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.EmailConfirmationTokenProvider, "EmailConfirmation", token);
        }
    }
}
