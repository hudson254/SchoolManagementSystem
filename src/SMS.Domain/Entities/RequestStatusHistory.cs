using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Append-only history entry for an SMS request lifecycle transition.
    /// </summary>
    [Table("sms_request_status_history")]
    public class RequestStatusHistory : BaseEntity, ITenantAwareEntity
    {
        [Column("request_id")]
        public Guid RequestId { get; private set; }

        public RequestStatus? FromStatus { get; private set; }
        public RequestStatus ToStatus { get; private set; }
        public RequestActionType Action { get; private set; }

        [Required]
        [MaxLength(100)]
        public string PerformedByUserId { get; private set; } = string.Empty;

        [MaxLength(256)]
        public string? PerformedByUsername { get; private set; }

        public DateTime PerformedAtUtc { get; private set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Remarks { get; private set; }

        /// <summary>Read-model alias kept for API/DTO compatibility.</summary>
        public string? Reason => Remarks;

        public virtual Request Request { get; private set; } = null!;

        private RequestStatusHistory() { }

        public static RequestStatusHistory Create(
            RequestStatus? fromStatus,
            RequestStatus toStatus,
            RequestActionType action,
            string performedByUserId,
            string? performedByUsername,
            string? remarks)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));

            return new RequestStatusHistory
            {
                FromStatus = fromStatus,
                ToStatus = toStatus,
                Action = action,
                PerformedByUserId = performedByUserId,
                PerformedByUsername = performedByUsername,
                Remarks = remarks,
                PerformedAtUtc = DateTime.UtcNow
            };
        }
    }
}
