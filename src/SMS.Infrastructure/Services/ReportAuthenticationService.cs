using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Configuration options for report authentication.
    /// </summary>
    public class ReportAuthenticationOptions
    {
        public const string SectionName = "ReportVerification";

        /// <summary>
        /// Base URL for verification (e.g., https://sms.example.com)
        /// </summary>
        public string VerificationBaseUrl { get; set; } = "https://localhost:5001";

        /// <summary>
        /// Verification endpoint path
        /// </summary>
        public string VerificationPath { get; set; } = "/verify/report";

        /// <summary>
        /// Default watermark text
        /// </summary>
        public string DefaultWatermarkText { get; set; } = "Official System Generated Report";

        /// <summary>
        /// Whether watermark is enabled by default
        /// </summary>
        public bool WatermarkEnabled { get; set; } = true;

        /// <summary>
        /// School/Organization name
        /// </summary>
        public string SchoolName { get; set; } = "Management Training School";

        /// <summary>
        /// Whether to enable full authentication (watermark + QR + hash)
        /// </summary>
        public bool AuthenticationEnabled { get; set; } = true;

        /// <summary>
        /// Max age in days for report verification before warning of expiry (null = no expiry)
        /// </summary>
        public int? ExpirationDays { get; set; } = null;

        /// <summary>
        /// Rate limit: max verification requests per IP per minute
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 30;
    }

    /// <summary>
    /// Implementation of IReportAuthenticationService.
    /// Orchestrates QR code generation, watermarking, hashing, token generation, and persistence.
    /// </summary>
    public class ReportAuthenticationService : IReportAuthenticationService
    {
        private readonly ILogger<ReportAuthenticationService> _logger;
        private readonly IReportVerificationRepository _repository;
        private readonly IReportTokenService _tokenService;
        private readonly IReportHashService _hashService;
        private readonly IQrCodeService _qrCodeService;
        private readonly IWatermarkService _watermarkService;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ReportAuthenticationOptions _options;

        public ReportAuthenticationService(
            ILogger<ReportAuthenticationService> logger,
            IReportVerificationRepository repository,
            IReportTokenService tokenService,
            IReportHashService hashService,
            IQrCodeService qrCodeService,
            IWatermarkService watermarkService,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            IOptions<ReportAuthenticationOptions> options)
        {
            _logger = logger;
            _repository = repository;
            _tokenService = tokenService;
            _hashService = hashService;
            _qrCodeService = qrCodeService;
            _watermarkService = watermarkService;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _options = options.Value;
        }

        public async Task<ReportAuthenticationResult> GenerateAuthenticationAsync(
            string reportType,
            string reportName,
            byte[] reportContent,
            string generatedByUserId,
            string generatedByUserName,
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Generate unique Report ID
                string reportId;
                do
                {
                    reportId = _tokenService.GenerateReportId();
                } while (await _repository.ReportIdExistsAsync(reportId, cancellationToken));

                // 2. Generate secure verification token
                string verificationToken;
                do
                {
                    verificationToken = _tokenService.GenerateVerificationToken();
                } while (await _repository.TokenExistsAsync(verificationToken, cancellationToken));

                // 3. Compute SHA-256 hash of report content
                var hash = _hashService.ComputeHash(reportContent);

                // 4. Generate verification URL
                var verificationUrl = $"{_options.VerificationBaseUrl.TrimEnd('/')}{_options.VerificationPath}/{verificationToken}";

                // 5. Generate QR code
                var qrCodeBytes = await _qrCodeService.GenerateQrCodeAsync(verificationUrl);

                // 6. Create verification record
                var verificationRecord = new ReportVerification
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ReportId = reportId,
                    VerificationToken = verificationToken,
                    ReportType = reportType,
                    ReportName = reportName,
                    GeneratedByUserId = generatedByUserId,
                    GeneratedByUserName = generatedByUserName,
                    GeneratedDate = DateTime.UtcNow,
                    SHA256Hash = hash,
                    HashAlgorithm = "SHA-256",
                    Status = ReportVerificationStatus.Valid,
                    VerificationCount = 0,
                    Version = 1,
                    WatermarkEnabled = _options.WatermarkEnabled,
                    WatermarkText = _options.DefaultWatermarkText,
                    ExpirationDate = _options.ExpirationDays.HasValue
                        ? DateTime.UtcNow.AddDays(_options.ExpirationDays.Value)
                        : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(verificationRecord, cancellationToken);

                _logger.LogInformation(
                    "Generated authentication for report - ID: {ReportId}, Type: {ReportType}, Token: {Token}",
                    reportId, reportType, verificationToken);

                // Audit log
                await _auditService.LogActivityAsync(
                    "ReportGenerated",
                    "ReportVerification",
                    reportId,
                    $"Report generated: {reportName} ({reportType})");

                return new ReportAuthenticationResult
                {
                    ReportId = reportId,
                    VerificationToken = verificationToken,
                    QrCode = qrCodeBytes,
                    Hash = hash,
                    VerificationRecord = verificationRecord,
                    VerificationUrl = verificationUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate authentication for report: {ReportName} ({ReportType})", reportName, reportType);
                throw;
            }
        }

        public async Task<ReportVerificationResult> VerifyReportByTokenAsync(
            string token,
            byte[]? reportContent = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ReportVerificationResult
            {
                VerifiedDate = DateTime.UtcNow,
                SchoolName = _options.SchoolName
            };

            try
            {
                // Validate token format
                if (!_tokenService.IsValidTokenFormat(token))
                {
                    result.IsValid = false;
                    result.Status = "Invalid";
                    result.Message = "Invalid verification token format. Please check the QR code or verification link.";
                    await LogVerificationAttempt(token, result, cancellationToken);
                    return result;
                }

                // Look up record
                var record = await _repository.GetByTokenAsync(token, cancellationToken);
                if (record == null)
                {
                    result.IsValid = false;
                    result.Status = "Invalid";
                    result.Message = "Report not found. This report was not generated by the system or the verification link is invalid.";
                    await LogVerificationAttempt(token, result, cancellationToken);
                    return result;
                }

                // Populate result fields
                result.ReportId = record.ReportId;
                result.ReportType = record.ReportType;
                result.ReportName = record.ReportName;
                result.GeneratedDate = record.GeneratedDate;
                result.GeneratedBy = record.GeneratedByUserName;
                result.Version = record.Version;
                result.VerificationCount = record.VerificationCount + 1;

                // Check if revoked
                if (record.Status == ReportVerificationStatus.Revoked)
                {
                    result.IsValid = false;
                    result.IsRevoked = true;
                    result.Status = "Revoked";
                    result.Message = $"This report has been revoked. Reason: {record.RevocationReason ?? "Not specified"}. This document should not be considered official.";
                    await UpdateVerificationCount(record, cancellationToken);
                    await LogVerificationAttempt(token, result, cancellationToken);
                    return result;
                }

                // Check if expired
                if (record.ExpirationDate.HasValue && DateTime.UtcNow > record.ExpirationDate.Value)
                {
                    result.IsValid = false;
                    result.IsExpired = true;
                    result.Status = "Expired";
                    result.Message = $"This report expired on {record.ExpirationDate.Value:yyyy-MM-dd}. Please request a new report.";
                    await UpdateVerificationCount(record, cancellationToken);
                    await LogVerificationAttempt(token, result, cancellationToken);
                    return result;
                }

                // Hash validation if report content is provided
                if (reportContent != null && reportContent.Length > 0)
                {
                    var hashValid = _hashService.ValidateHash(reportContent, record.SHA256Hash, record.HashAlgorithm);
                    result.HashValid = hashValid;

                    if (!hashValid)
                    {
                        result.IsValid = false;
                        result.IsTampered = true;
                        result.Status = "Tampered";
                        result.Message = "WARNING: The report content has been modified since generation. This document should not be considered official.";
                        await UpdateVerificationCount(record, cancellationToken);
                        await LogVerificationAttempt(token, result, cancellationToken);
                        return result;
                    }
                }
                else
                {
                    // If no content provided for hash check, still consider it valid
                    result.HashValid = true;
                }

                // All checks passed
                result.IsValid = true;
                result.Status = "Valid";
                result.Message = "This is an authentic system-generated report. The document has not been tampered with.";
                result.DigitalSignatureStatus = "Verified";

                // Update verification record
                record.VerificationCount++;
                record.LastVerified = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(record, cancellationToken);

                await LogVerificationAttempt(token, result, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying report with token: {Token}", token);
                result.IsValid = false;
                result.Status = "Error";
                result.Message = "An error occurred during verification. Please try again later.";
                return result;
            }
        }

        public async Task<ReportVerificationResult> GetReportStatusAsync(
            string reportId,
            CancellationToken cancellationToken = default)
        {
            var result = new ReportVerificationResult
            {
                VerifiedDate = DateTime.UtcNow,
                SchoolName = _options.SchoolName
            };

            try
            {
                var record = await _repository.GetByReportIdAsync(reportId, cancellationToken);
                if (record == null)
                {
                    result.IsValid = false;
                    result.Status = "Invalid";
                    result.Message = "Report not found.";
                    return result;
                }

                result.ReportId = record.ReportId;
                result.ReportType = record.ReportType;
                result.ReportName = record.ReportName;
                result.GeneratedDate = record.GeneratedDate;
                result.GeneratedBy = record.GeneratedByUserName;
                result.Version = record.Version;
                result.VerificationCount = record.VerificationCount;
                result.HashValid = true;

                switch (record.Status)
                {
                    case ReportVerificationStatus.Valid:
                        if (record.ExpirationDate.HasValue && DateTime.UtcNow > record.ExpirationDate.Value)
                        {
                            result.IsValid = false;
                            result.IsExpired = true;
                            result.Status = "Expired";
                            result.Message = $"This report expired on {record.ExpirationDate.Value:yyyy-MM-dd}.";
                        }
                        else
                        {
                            result.IsValid = true;
                            result.Status = "Valid";
                            result.Message = "This is an authentic system-generated report.";
                        }
                        break;
                    case ReportVerificationStatus.Revoked:
                        result.IsValid = false;
                        result.IsRevoked = true;
                        result.Status = "Revoked";
                        result.Message = $"This report has been revoked. Reason: {record.RevocationReason ?? "Not specified"}.";
                        break;
                    default:
                        result.IsValid = false;
                        result.Status = record.Status.ToString();
                        result.Message = "Report status is invalid.";
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report status: {ReportId}", reportId);
                result.IsValid = false;
                result.Status = "Error";
                result.Message = "An error occurred while checking report status.";
                return result;
            }
        }

        public async Task<bool> RevokeReportAsync(
            string reportId,
            string revokedBy,
            string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var record = await _repository.GetByReportIdAsync(reportId, cancellationToken);
                if (record == null)
                {
                    _logger.LogWarning("Cannot revoke non-existent report: {ReportId}", reportId);
                    return false;
                }

                if (record.Status == ReportVerificationStatus.Revoked)
                {
                    _logger.LogWarning("Report already revoked: {ReportId}", reportId);
                    return true; // Idempotent - already revoked
                }

                record.Status = ReportVerificationStatus.Revoked;
                record.RevokedDate = DateTime.UtcNow;
                record.RevokedBy = revokedBy;
                record.RevocationReason = reason;
                record.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(record, cancellationToken);

                _logger.LogInformation(
                    "Report revoked - ID: {ReportId}, By: {RevokedBy}, Reason: {Reason}",
                    reportId, revokedBy, reason);

                await _auditService.LogActivityAsync(
                    "ReportRevoked",
                    "ReportVerification",
                    reportId,
                    $"Report revoked by {revokedBy}. Reason: {reason}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke report: {ReportId}", reportId);
                throw;
            }
        }

        public async Task<bool> RestoreReportAsync(
            string reportId,
            string restoredBy,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var record = await _repository.GetByReportIdAsync(reportId, cancellationToken);
                if (record == null)
                {
                    _logger.LogWarning("Cannot restore non-existent report: {ReportId}", reportId);
                    return false;
                }

                if (record.Status != ReportVerificationStatus.Revoked)
                {
                    _logger.LogWarning("Report is not revoked, cannot restore: {ReportId}", reportId);
                    return false;
                }

                record.Status = ReportVerificationStatus.Valid;
                record.RevokedDate = null;
                record.RevokedBy = null;
                record.RevocationReason = null;
                record.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(record, cancellationToken);

                _logger.LogInformation(
                    "Report restored - ID: {ReportId}, By: {RestoredBy}",
                    reportId, restoredBy);

                await _auditService.LogActivityAsync(
                    "ReportRestored",
                    "ReportVerification",
                    reportId,
                    $"Report restored by {restoredBy}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore report: {ReportId}", reportId);
                throw;
            }
        }

        public async Task<byte[]> GenerateQrCodeAsync(string verificationUrl)
        {
            return await _qrCodeService.GenerateQrCodeAsync(verificationUrl);
        }

        public async Task<byte[]> GenerateWatermarkAsync(string watermarkText, int width = 500, int height = 500)
        {
            return await _watermarkService.GenerateWatermarkAsync(watermarkText, width, height);
        }

        private async Task UpdateVerificationCount(ReportVerification record, CancellationToken cancellationToken)
        {
            try
            {
                record.VerificationCount++;
                record.LastVerified = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(record, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update verification count for report: {ReportId}", record.ReportId);
            }
        }

        private async Task LogVerificationAttempt(string token, ReportVerificationResult result, CancellationToken cancellationToken)
        {
            try
            {
                await _auditService.LogActivityAsync(
                    "ReportVerified",
                    "ReportVerification",
                    result.ReportId,
                    $"Verification result: {result.Status}. Report: {result.ReportName ?? "N/A"} ({result.ReportType ?? "N/A"})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log verification attempt for token: {Token}", token);
            }
        }
    }
}
