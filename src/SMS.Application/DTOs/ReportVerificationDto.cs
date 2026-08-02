using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// DTO for report verification response
    /// </summary>
    public class ReportVerificationResponseDto
    {
        public bool IsValid { get; set; }
        public string ReportId { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public DateTime VerifiedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool HashValid { get; set; }
        public string DigitalSignatureStatus { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Message { get; set; } = string.Empty;
        public int VerificationCount { get; set; }
        public bool IsExpired { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsTampered { get; set; }
    }

    /// <summary>
    /// DTO for report authentication generation result
    /// </summary>
    public class ReportAuthenticationDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string VerificationToken { get; set; } = string.Empty;
        public byte[] QrCode { get; set; } = Array.Empty<byte>();
        public string Hash { get; set; } = string.Empty;
        public string VerificationUrl { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// DTO for report verification record in admin list
    /// </summary>
    public class ReportVerificationRecordDto
    {
        public Guid Id { get; set; }
        public string ReportId { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string GeneratedByUserName { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int VerificationCount { get; set; }
        public DateTime? LastVerified { get; set; }
        public DateTime? RevokedDate { get; set; }
        public string? RevokedBy { get; set; }
        public string? RevocationReason { get; set; }
        public int Version { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    /// <summary>
    /// DTO for paginated list of verification records
    /// </summary>
    public class ReportVerificationListDto
    {
        public List<ReportVerificationRecordDto> Records { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// DTO for verification history entry
    /// </summary>
    public class VerificationHistoryEntryDto
    {
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for report revocation request
    /// </summary>
    public class RevokeReportRequestDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for report verification search parameters
    /// </summary>
    public class ReportVerificationSearchDto
    {
        public string? ReportType { get; set; }
        public string? ReportId { get; set; }
        public string? GeneratedBy { get; set; }
        public ReportVerificationStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// DTO for report verification settings
    /// </summary>
    public class ReportVerificationSettingsDto
    {
        public bool AuthenticationEnabled { get; set; } = true;
        public bool WatermarkEnabled { get; set; } = true;
        public string WatermarkText { get; set; } = "Official System Generated Report";
        public string VerificationBaseUrl { get; set; } = string.Empty;
        public string SchoolName { get; set; } = "Management Training School";
        public int? ExpirationDays { get; set; }
        public int RateLimitPerMinute { get; set; } = 30;
        public List<ReportTypeSettingDto> ReportTypeSettings { get; set; } = new();
    }

    /// <summary>
    /// DTO for per-report-type settings
    /// </summary>
    public class ReportTypeSettingDto
    {
        public string ReportType { get; set; } = string.Empty;
        public bool WatermarkEnabled { get; set; } = true;
        public bool AuthenticationEnabled { get; set; } = true;
    }
}
