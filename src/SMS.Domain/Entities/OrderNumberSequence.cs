using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Per-tenant, per-year order-number sequence backing
    /// <c>IOrderNumberGenerator</c> (open requirement #1). Allocated with a single
    /// atomic INSERT ... ON CONFLICT ... UPDATE ... RETURNING statement so
    /// concurrent order creation can never produce a duplicate number.
    /// Infrastructure table — not exposed through domain behavior.
    /// </summary>
    [Table("oms_order_sequences")]
    public class OrderNumberSequence
    {
        [Column("tenant_id")]
        public Guid TenantId { get; set; }

        [Column("year")]
        public int Year { get; set; }

        [Column("last_number")]
        public long LastNumber { get; set; }
    }
}
