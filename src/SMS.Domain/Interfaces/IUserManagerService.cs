using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IUserManagerService
    {
        // User Retrieval
        Task<User> FindByIdAsync(string userId);
        Task<User> FindByEmailAsync(string email);
        Task<User> FindByUsernameAsync(string username);
        Task<User> FindByRefreshTokenAsync(string refreshToken);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);

        // User Authentication
        Task<bool> ValidateUserCredentialsAsync(string username, string password);
        Task<bool> CheckPasswordAsync(User user, string password);

        // User Creation & Management
        Task<User> CreateUserAsync(string username, string email, string password, string role = null);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> UpdateUserAsync(string userId, object userData);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> LockUserAsync(string userId, string reason = null);
        Task<bool> UnlockUserAsync(string userId);

        // Password Management
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(User user);

        // Email Verification
        Task<bool> VerifyEmailAsync(string userId, string token);
        Task<string> GenerateEmailVerificationTokenAsync(User user);
        Task<bool> ConfirmEmailAsync(User user, string token);

        // Role Management
        Task<bool> AssignRoleAsync(string userId, string role);
        Task<bool> AddToRoleAsync(User user, string role);
        Task<bool> RemoveRoleAsync(string userId, string role);
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);
        Task<IEnumerable<string>> GetRolesAsync(User user);
        Task<IEnumerable<string>> GetPermissionsAsync(User user);

        // Token Management
        Task<string> GenerateRefreshTokenAsync(string userId);
        Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string userId);

        // Refresh Token Reuse Detection
        Task<bool> IsRefreshTokenReusedAsync(string userId, string refreshToken);
        Task<bool> RevokeRefreshTokenFamilyAsync(string userId);
        Task<string> RotateRefreshTokenAsync(string userId, string currentRefreshToken);

        // Password Reset Revocation
        Task<bool> RevokeAllRefreshTokensAsync(string userId);

        // Session Management
        Task<bool> LogoutAsync(string userId);
        Task<bool> IsUserOnlineAsync(string userId);
        Task<DateTime?> GetLastLoginTimeAsync(string userId);

        // User Status
        Task<bool> UserExistsAsync(string userId);
        Task<bool> IsUserActiveAsync(string userId);
        Task<bool> IsEmailConfirmedAsync(string userId);

        // Missing methods used by handlers
        Task<User> GetUserByIdAsync(string userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> ValidatePasswordResetTokenAsync(string email, string token);
        Task<bool> ValidateEmailVerificationTokenAsync(string email, string token);
    }
}
