using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.ReportVerification.Commands
{
    /// <summary>
    /// Command to generate authentication credentials for a report.
    /// </summary>
    public class GenerateReportAuthenticationCommand : IRequest<ReportAuthenticationDto>
    {
        public string ReportType { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public byte[] ReportContent { get; set; } = Array.Empty<byte>();
        public string GeneratedByUserId { get; set; } = string.Empty;
        public string GeneratedByUserName { get; set; } = string.Empty;
    }

    public class GenerateReportAuthenticationHandler : IRequestHandler<GenerateReportAuthenticationCommand, ReportAuthenticationDto>
    {
        private readonly IReportAuthenticationService _authService;
        private readonly ILogger<GenerateReportAuthenticationHandler> _logger;

        public GenerateReportAuthenticationHandler(
            IReportAuthenticationService authService,
            ILogger<GenerateReportAuthenticationHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<ReportAuthenticationDto> Handle(GenerateReportAuthenticationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.GenerateAuthenticationAsync(
                    request.ReportType,
                    request.ReportName,
                    request.ReportContent,
                    request.GeneratedByUserId,
                    request.GeneratedByUserName,
                    Guid.Empty, // TenantId will be set by the service
                    cancellationToken);

                _logger.LogInformation(
                    "Authentication generated for report: {ReportName} ({ReportType}) - ID: {ReportId}",
                    request.ReportName, request.ReportType, result.ReportId);

                return new ReportAuthenticationDto
                {
                    ReportId = result.ReportId,
                    VerificationToken = result.VerificationToken,
                    QrCode = result.QrCode,
                    Hash = result.Hash,
                    VerificationUrl = result.VerificationUrl,
                    GeneratedDate = result.VerificationRecord.GeneratedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate authentication for report: {ReportName} ({ReportType})",
                    request.ReportName, request.ReportType);
                throw;
            }
        }
    }
}
