using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Configurable request type definition.
    /// Defines the workflow and permissions for different types of SMS requests.
    /// </summary>
    [Table("sms_request_types")]
    public class RequestType : BaseEntity, ITenantAwareEntity
    {
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Allowed requester roles (comma-separated role names)</summary>
        [MaxLength(500)]
        public string? AllowedRequesterRoles { get; set; }

        /// <summary>Approver roles (comma-separated role names)</summary>
        [MaxLength(500)]
        public string? ApproverRoles { get; set; }

        /// <summary>Workflow steps in order (comma-separated status names)</summary>
        [MaxLength(500)]
        public string? WorkflowSteps { get; set; }

        /// <summary>Whether attachments are required</summary>
        public bool RequiresAttachments { get; set; } = false;

        /// <summary>Whether comments are enabled</summary>
        public bool EnableComments { get; set; } = true;

        /// <summary>Default priority for this request type</summary>
        public RequestPriority DefaultPriority { get; set; } = RequestPriority.Normal;

        /// <summary>Whether to notify on submission</summary>
        public bool NotifyOnSubmit { get; set; } = true;

        /// <summary>Whether to notify on approval</summary>
        public bool NotifyOnApprove { get; set; } = true;

        /// <summary>Whether to notify on rejection</summary>
        public bool NotifyOnReject { get; set; } = true;

        /// <summary>Whether to notify on return</summary>
        public bool NotifyOnReturn { get; set; } = true;

        public static RequestType Create(string code, string displayName, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required.", nameof(code));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));

            return new RequestType
            {
                Code = code,
                DisplayName = displayName,
                Description = description
            };
        }
    }
}
