using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.ReportVerification.Queries
{
    /// <summary>
    /// Query to verify a report using its verification token.
    /// Public endpoint - no authorization required.
    /// </summary>
    public class VerifyReportByTokenQuery : IRequest<ReportVerificationResponseDto>
    {
        /// <summary>
        /// The verification token from the QR code
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Optional report content to verify hash integrity
        /// </summary>
        public byte[]? ReportContent { get; set; }
    }

    public class VerifyReportByTokenHandler : IRequestHandler<VerifyReportByTokenQuery, ReportVerificationResponseDto>
    {
        private readonly IReportAuthenticationService _authService;
        private readonly ILogger<VerifyReportByTokenHandler> _logger;

        public VerifyReportByTokenHandler(
            IReportAuthenticationService authService,
            ILogger<VerifyReportByTokenHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<ReportVerificationResponseDto> Handle(VerifyReportByTokenQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.VerifyReportByTokenAsync(
                    request.Token,
                    request.ReportContent,
                    cancellationToken);

                _logger.LogInformation(
                    "Report verification: {Status} - Report: {ReportId} ({ReportType})",
                    result.Status, result.ReportId, result.ReportType);

                return new ReportVerificationResponseDto
                {
                    IsValid = result.IsValid,
                    ReportId = result.ReportId,
                    ReportType = result.ReportType,
                    ReportName = result.ReportName,
                    GeneratedDate = result.GeneratedDate,
                    VerifiedDate = result.VerifiedDate,
                    GeneratedBy = result.GeneratedBy,
                    SchoolName = result.SchoolName,
                    Status = result.Status,
                    HashValid = result.HashValid,
                    DigitalSignatureStatus = result.DigitalSignatureStatus,
                    Version = result.Version,
                    Message = result.Message,
                    VerificationCount = result.VerificationCount,
                    IsExpired = result.IsExpired,
                    IsRevoked = result.IsRevoked,
                    IsTampered = result.IsTampered
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying report with token");
                return new ReportVerificationResponseDto
                {
                    IsValid = false,
                    Status = "Error",
                    Message = "An error occurred during verification. Please try again later."
                };
            }
        }
    }
}
