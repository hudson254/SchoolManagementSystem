using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Features.Notifications.Commands;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Notifications.Services
{
    /// <summary>
    /// Service that creates notifications for registration workflow events.
    /// </summary>
    public class RegistrationNotificationService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RegistrationNotificationService> _logger;

        public RegistrationNotificationService(
            IMediator mediator,
            ILogger<RegistrationNotificationService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Notify a user that their enrollment/assignment has been submitted for approval.
        /// </summary>
        public async Task NotifySubmissionAsync(string userId, string userType)
        {
            var command = new CreateNotificationCommand
            {
                UserId = userId,
                Title = "Registration Submitted for Approval",
                Message = $"Your {userType} registration has been submitted and is awaiting approval.",
                Type = "Registration",
                ReferenceId = userId
            };

            await _mediator.Send(command);
            _logger.LogInformation("Submission notification sent to {UserType} {UserId}", userType, userId);
        }

        /// <summary>
        /// Notify a user that their registration has been approved.
        /// </summary>
        public async Task NotifyApprovalAsync(string userId, string userType)
        {
            var command = new CreateNotificationCommand
            {
                UserId = userId,
                Title = "Registration Approved",
                Message = $"Your {userType} registration has been approved. You now have full access.",
                Type = "Registration",
                ReferenceId = userId
            };

            await _mediator.Send(command);
            _logger.LogInformation("Approval notification sent to {UserType} {UserId}", userType, userId);
        }

        /// <summary>
        /// Notify a user that their registration has been rejected.
        /// </summary>
        public async Task NotifyRejectionAsync(string userId, string userType, string reason)
        {
            var command = new CreateNotificationCommand
            {
                UserId = userId,
                Title = "Registration Rejected",
                Message = $"Your {userType} registration has been rejected. Reason: {reason}",
                Type = "Registration",
                ReferenceId = userId
            };

            await _mediator.Send(command);
            _logger.LogInformation("Rejection notification sent to {UserType} {UserId}: {Reason}", userType, userId, reason);
        }

        /// <summary>
        /// Notify admins that there are pending registrations to review.
        /// </summary>
        public async Task NotifyAdminsPendingApprovalAsync(int pendingCount)
        {
            // This would typically be sent to admin users
            _logger.LogInformation("There are {PendingCount} pending registrations awaiting approval", pendingCount);
        }
    }
}
