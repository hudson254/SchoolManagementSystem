using System;
using SMS.Domain.Common;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a user's request for a password reset, to be fulfilled by
    /// an administrator. Replaces the previous email-link-based self-service
    /// reset flow, which required SMTP (now fully removed from the system).
    /// </summary>
    public class PasswordResetRequest : BaseEntity
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The user who requested the reset (null if the user was not found,
        /// to prevent account enumeration).
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// The email or username submitted by the requester.
        /// </summary>
        public string RequestedEmail { get; set; } = string.Empty;

        /// <summary>
        /// Optional note from the requester.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// When the request was submitted.
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Pending = awaiting admin action;
        /// Fulfilled = admin set a new password;
        /// Rejected = admin declined the request.
        /// </summary>
        public PasswordResetRequestStatus Status { get; set; } = PasswordResetRequestStatus.Pending;

        /// <summary>
        /// The admin user who fulfilled or rejected the request.
        /// </summary>
        public string? FulfilledByUserId { get; set; }

        /// <summary>
        /// When the request was fulfilled or rejected.
        /// </summary>
        public DateTime? FulfilledAt { get; set; }

        /// <summary>
        /// Optional reason for rejection or fulfillment note.
        /// </summary>
        public string? ResolutionNote { get; set; }
    }

    public enum PasswordResetRequestStatus
    {
        Pending = 0,
        Fulfilled = 1,
        Rejected = 2
    }
}
