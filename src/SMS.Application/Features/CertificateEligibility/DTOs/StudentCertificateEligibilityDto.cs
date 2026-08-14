using SMS.Domain.Enums;

namespace SMS.Application.Features.CertificateEligibility.DTOs
{
    public class StudentCertificateEligibilityDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public CertificateEligibilityStatus Status { get; set; }
        public string? IneligibilityReason { get; set; }
        public DateTime? EvaluatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<string> MissingRequirements { get; set; } = new();
        public decimal OverallPercentage { get; set; }
        public string OverallGrade { get; set; } = string.Empty;
    }
}



