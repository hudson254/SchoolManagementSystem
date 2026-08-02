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
    /// Query to get the verification status of a report by its Report ID.
    /// </summary>
    public class GetReportStatusQuery : IRequest<ReportVerificationResponseDto>
    {
        public string ReportId { get; set; } = string.Empty;
    }

    public class GetReportStatusHandler : IRequestHandler<GetReportStatusQuery, ReportVerificationResponseDto>
    {
        private readonly IReportAuthenticationService _authService;
        private readonly ILogger<GetReportStatusHandler> _logger;

        public GetReportStatusHandler(
            IReportAuthenticationService authService,
            ILogger<GetReportStatusHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<ReportVerificationResponseDto> Handle(GetReportStatusQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.GetReportStatusAsync(request.ReportId, cancellationToken);

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
                _logger.LogError(ex, "Error getting report status: {ReportId}", request.ReportId);
                return new ReportVerificationResponseDto
                {
                    IsValid = false,
                    Status = "Error",
                    Message = "An error occurred while checking report status."
                };
            }
        }
    }
}
