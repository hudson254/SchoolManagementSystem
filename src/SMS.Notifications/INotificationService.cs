using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Notifications
{
    /// <summary>
    /// Service for sending notifications via multiple channels (in-app, email, SMS)
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a real-time in-app notification to a specific user
        /// </summary>
        Task SendNotificationAsync(string userId, string title, string message, string? type = null, string? referenceId = null);

        /// <summary>
        /// Sends an email notification to a user
        /// </summary>
        Task SendEmailNotificationAsync(string userId, string email, string subject, string body);

        /// <summary>
        /// Sends an SMS notification to a user
        /// </summary>
        Task SendSmsNotificationAsync(string userId, string phoneNumber, string message);

        /// <summary>
        /// Broadcasts a notification to multiple users
        /// </summary>
        Task BroadcastNotificationAsync(string title, string message, IEnumerable<string> userIds, string? type = null);

        /// <summary>
        /// Sends a notification to all users with a specific role
        /// </summary>
        Task SendRoleNotificationAsync(string title, string message, string role, string? type = null);

        /// <summary>
        /// Sends a templated email notification
        /// </summary>
        Task SendTemplatedEmailAsync(string userId, string email, string templateName, Dictionary<string, string> templateData);

        /// <summary>
        /// Sends a password reset notification (email + in-app)
        /// </summary>
        Task SendPasswordResetNotificationAsync(string userId, string email, string resetLink);

        /// <summary>
        /// Sends an email verification notification
        /// </summary>
        Task SendVerificationNotificationAsync(string userId, string email, string verificationLink);
    }
}

