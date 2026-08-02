using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Notifications.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string? type = null, string? referenceId = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type ?? "System",
                ReferenceId = referenceId,
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };

            // Send real-time notification via SignalR
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
        }

        public async Task SendEmailNotificationAsync(string userId, string email, string subject, string body)
        {
            try
            {
                await _emailService.SendEmailAsync(email, subject, body);
                _logger.LogInformation("Email notification sent to {Email} for user {UserId}", email, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification to {Email} for user {UserId}", email, userId);
                throw;
            }
        }

        public async Task SendSmsNotificationAsync(string userId, string phoneNumber, string message)
        {
            try
            {
                await _smsService.SendSmsAsync(phoneNumber, message);
                _logger.LogInformation("SMS notification sent to {PhoneNumber} for user {UserId}", phoneNumber, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS notification to {PhoneNumber} for user {UserId}", phoneNumber, userId);
                throw;
            }
        }

        public async Task BroadcastNotificationAsync(string title, string message, IEnumerable<string> userIds, string? type = null)
        {
            var tasks = userIds.Select(userId => SendNotificationAsync(userId, title, message, type));
            await Task.WhenAll(tasks);
            _logger.LogInformation("Broadcast notification sent to {Count} users", userIds.Count());
        }

        public async Task SendRoleNotificationAsync(string title, string message, string role, string? type = null)
        {
            // Send to SignalR group for the role
            await _hubContext.Clients.Group($"role_{role}").SendAsync("ReceiveNotification", new
            {
                Title = title,
                Message = message,
                Type = type ?? "System",
                CreatedDate = DateTime.UtcNow
            });

            _logger.LogInformation("Role notification sent to role {Role}: {Title}", role, title);
        }

        public async Task SendTemplatedEmailAsync(string userId, string email, string templateName, Dictionary<string, string> templateData)
        {
            try
            {
                await _emailService.SendTemplateEmailAsync(email, templateName, templateData);
                _logger.LogInformation("Templated email {Template} sent to {Email} for user {UserId}", templateName, email, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send templated email {Template} to {Email} for user {UserId}", templateName, email, userId);
                throw;
            }
        }

        public async Task SendPasswordResetNotificationAsync(string userId, string email, string resetLink)
        {
            try
            {
                await _emailService.SendPasswordResetEmailAsync(email, resetLink);

                await SendNotificationAsync(userId, "Password Reset", "A password reset link has been sent to your email.", "Security");

                _logger.LogInformation("Password reset notification sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset notification to user {UserId}", userId);
                throw;
            }
        }

        public async Task SendVerificationNotificationAsync(string userId, string email, string verificationLink)
        {
            try
            {
                await _emailService.SendVerificationEmailAsync(email, verificationLink);

                await SendNotificationAsync(userId, "Email Verification", "A verification link has been sent to your email.", "Security");

                _logger.LogInformation("Verification notification sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification notification to user {UserId}", userId);
                throw;
            }
        }
    }
}

