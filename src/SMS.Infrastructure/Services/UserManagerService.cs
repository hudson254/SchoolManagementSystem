using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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

        public UserManagerService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ILogger<UserManagerService> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _context = context;
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
            var users = await _userManager.Users
                .Where(u => u.RefreshToken == refreshToken)
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
                RefreshToken = string.Empty,
                EmailConfirmed = true,
                IsEmailVerified = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create user: {Errors}", string.Join(", ", result.Errors));
                return null;
            }

            if (!string.IsNullOrEmpty(role))
                await AddToRoleAsync(user, role);

            return user;
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
            var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);
            }
            return refreshToken;
        }

        public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(refreshToken)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return user.RefreshToken == refreshToken && user.RefreshTokenExpiryTime > DateTime.UtcNow;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            return (await _userManager.UpdateAsync(user)).Succeeded;
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
