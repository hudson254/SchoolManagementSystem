using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Append-only history entry for an OMS order lifecycle transition.
    /// Entries are created exclusively by <see cref="Order"/> transitions via
    /// <see cref="Create"/>; there is intentionally no public mutation surface
    /// (no update/delete methods) so the audit trail cannot be rewritten
    /// through ordinary entity operations.
    /// </summary>
    [Table("oms_order_status_history")]
    public class OrderStatusHistory : BaseEntity, ITenantAwareEntity
    {
        [Column("order_id")]
        public Guid OrderId { get; private set; }

        /// <summary>Previous status (null only for the initial creation entry).</summary>
        public OrderStatus? FromStatus { get; private set; }

        public OrderStatus ToStatus { get; private set; }

        public OrderActionType Action { get; private set; }

        [Required]
        [MaxLength(100)]
        public string PerformedByUserId { get; private set; } = string.Empty;

        [MaxLength(256)]
        public string? PerformedByUsername { get; private set; }

        public DateTime PerformedAtUtc { get; private set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Remarks { get; private set; }

        public virtual Order Order { get; private set; } = null!;

        private OrderStatusHistory()
        {
        }

        /// <summary>Creates an immutable history entry (append-only).</summary>
        public static OrderStatusHistory Create(
            OrderStatus? fromStatus,
            OrderStatus toStatus,
            OrderActionType action,
            string performedByUserId,
            string? performedByUsername,
            string? remarks)
        {
            if (string.IsNullOrWhiteSpace(performedByUserId))
            {
                throw new ArgumentException("Performing user is required.", nameof(performedByUserId));
            }

            return new OrderStatusHistory
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
