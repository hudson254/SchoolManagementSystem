using SMS.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Service interface for report authentication operations.
    /// Provides methods for generating, verifying, and managing report authentication.
    /// </summary>
    public interface IReportAuthenticationService
    {
        /// <summary>
        /// Generates a complete authentication package for a report.
        /// Creates ReportId, verification token, QR code, hash, and persists the record.
        /// </summary>
        /// <param name="reportType">Type of the report</param>
        /// <param name="reportName">Name/title of the report</param>
        /// <param name="reportContent">Byte content of the report for hashing</param>
        /// <param name="generatedByUserId">ID of the user generating the report</param>
        /// <param name="generatedByUserName">Name of the user generating the report</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication result with ReportId, token, QR code bytes, and verification record</returns>
        Task<ReportAuthenticationResult> GenerateAuthenticationAsync(
            string reportType,
            string reportName,
            byte[] reportContent,
            string generatedByUserId,
            string generatedByUserName,
            Guid tenantId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a report using its verification token.
        /// </summary>
        /// <param name="token">The verification token</param>
        /// <param name="reportContent">Optional report content to verify hash integrity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Verification result with status and metadata</returns>
        Task<ReportVerificationResult> VerifyReportByTokenAsync(
            string token,
            byte[]? reportContent = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the verification status of a report by its Report ID.
        /// </summary>
        Task<ReportVerificationResult> GetReportStatusAsync(
            string reportId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a report, making it fail verification.
        /// </summary>
        /// <param name="reportId">The Report ID to revoke</param>
        /// <param name="revokedBy">ID of the administrator revoking the report</param>
        /// <param name="reason">Reason for revocation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if revocation was successful</returns>
        Task<bool> RevokeReportAsync(
            string reportId,
            string revokedBy,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a previously revoked report.
        /// </summary>
        /// <param name="reportId">The Report ID to restore</param>
        /// <param name="restoredBy">ID of the administrator restoring the report</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if restoration was successful</returns>
        Task<bool> RestoreReportAsync(
            string reportId,
            string restoredBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a QR code as a byte array (PNG) for a given verification URL.
        /// </summary>
        /// <param name="verificationUrl">The URL to encode in the QR code</param>
        /// <returns>PNG byte array of the QR code</returns>
        Task<byte[]> GenerateQrCodeAsync(string verificationUrl);

        /// <summary>
        /// Generates a watermark image as a byte array.
        /// </summary>
        /// <param name="watermarkText">Text to use as watermark</param>
        /// <param name="width">Width of the watermark image</param>
        /// <param name="height">Height of the watermark image</param>
        /// <returns>PNG byte array of the watermark</returns>
        Task<byte[]> GenerateWatermarkAsync(string watermarkText, int width = 500, int height = 500);
    }

    /// <summary>
    /// Result of report authentication generation.
    /// </summary>
    public class ReportAuthenticationResult
    {
        /// <summary>Globally unique Report ID</summary>
        public string ReportId { get; set; } = string.Empty;

        /// <summary>Cryptographic verification token</summary>
        public string VerificationToken { get; set; } = string.Empty;

        /// <summary>QR code as PNG byte array</summary>
        public byte[] QrCode { get; set; } = Array.Empty<byte>();

        /// <summary>SHA-256 hash of the report content</summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>The verification record</summary>
        public ReportVerification VerificationRecord { get; set; } = null!;

        /// <summary>Verification URL for QR code</summary>
        public string VerificationUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of a verification operation.
    /// </summary>
    public class ReportVerificationResult
    {
        /// <summary>Whether the verification was successful</summary>
        public bool IsValid { get; set; }

        /// <summary>Report ID</summary>
        public string ReportId { get; set; } = string.Empty;

        /// <summary>Report type</summary>
        public string ReportType { get; set; } = string.Empty;

        /// <summary>Report title/name</summary>
        public string ReportName { get; set; } = string.Empty;

        /// <summary>Date the report was generated</summary>
        public DateTime GeneratedDate { get; set; }

        /// <summary>Date of this verification</summary>
        public DateTime VerifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>Name of the user who generated the report</summary>
        public string GeneratedBy { get; set; } = string.Empty;

        /// <summary>School/Tenant name</summary>
        public string SchoolName { get; set; } = string.Empty;

        /// <summary>Current verification status</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Hash validation result</summary>
        public bool HashValid { get; set; }

        /// <summary>Digital signature status</summary>
        public string DigitalSignatureStatus { get; set; } = "Not Applicable";

        /// <summary>Version number of the report</summary>
        public int Version { get; set; } = 1;

        /// <summary>Human-readable message describing the result</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Number of times this report has been verified</summary>
        public int VerificationCount { get; set; }

        /// <summary>Whether the report has expired</summary>
        public bool IsExpired { get; set; }

        /// <summary>Whether the report has been revoked</summary>
        public bool IsRevoked { get; set; }

        /// <summary>Whether the report has been tampered with</summary>
        public bool IsTampered { get; set; }
    }
}
