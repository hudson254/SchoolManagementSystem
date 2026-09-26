using SMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Per-tenant, per-year sequence for request number generation (REQ-yyyy-nnnnnn).
    /// </summary>
    [Table("sms_request_number_sequences")]
    public class RequestNumberSequence : BaseEntity, ITenantAwareEntity
    {
        public int Year { get; set; }
        public long LastNumber { get; set; }

        private RequestNumberSequence() { }

        public static RequestNumberSequence Create(Guid tenantId, int year, long lastNumber = 1)
        {
            return new RequestNumberSequence
            {
                TenantId = tenantId,
                Year = year,
                LastNumber = lastNumber
            };
        }
    }
}
