using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Tracks the certificate eligibility status for a student.
    /// </summary>
    public class StudentCertificateEligibility : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }
        public Guid? CertificateRuleId { get; set; }
        public CertificateEligibilityStatus Status { get; set; } = CertificateEligibilityStatus.NotDetermined;
        public decimal? OverallPercentage { get; set; }
        public string? OverallGradeLetter { get; set; }
        public bool HasOutstandingIncomplete { get; set; }
        public bool HasFailedRequiredUnits { get; set; }
        public string? EligibilityDetails { get; set; }
        public DateTime? EvaluatedDate { get; set; }
        public string? EvaluatedBy { get; set; }

        public virtual Student Student { get; set; }
        public virtual CertificateRule CertificateRule { get; set; }
    }
}
