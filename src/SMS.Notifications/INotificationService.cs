using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Notifications
{
    /// <summary>
    /// Service for sending in-app notifications to system users.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a real-time in-app notification to a specific user.
        /// </summary>
        Task SendNotificationAsync(string userId, string title, string message, string? type = null, string? referenceId = null);

        /// <summary>
        /// Broadcasts a notification to multiple users.
        /// </summary>
        Task BroadcastNotificationAsync(string title, string message, IEnumerable<string> userIds, string? type = null);

        /// <summary>
        /// Sends a notification to all users with a specific role.
        /// </summary>
        Task SendRoleNotificationAsync(string title, string message, string role, string? type = null);

        /// <summary>
        /// Sends a password reset notification to the user.
        /// </summary>
        Task SendPasswordResetNotificationAsync(string userId, string email, string resetLink);

        /// <summary>
        /// Sends a verification notification to the user.
        /// </summary>
        Task SendVerificationNotificationAsync(string userId, string email, string verificationLink);
    }
}

