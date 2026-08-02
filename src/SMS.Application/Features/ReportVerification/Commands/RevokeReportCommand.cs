using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.ReportVerification.Commands
{
    /// <summary>
    /// Command to revoke a report, making it fail verification.
    /// </summary>
    public class RevokeReportCommand : IRequest<bool>
    {
        public string ReportId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RevokeReportHandler : IRequestHandler<RevokeReportCommand, bool>
    {
        private readonly IReportAuthenticationService _authService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<RevokeReportHandler> _logger;

        public RevokeReportHandler(
            IReportAuthenticationService authService,
            SMS.Domain.Interfaces.ICurrentUserService currentUserService,
            ILogger<RevokeReportHandler> logger)
        {
            _authService = authService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(RevokeReportCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.RevokeReportAsync(
                    request.ReportId,
                    _currentUserService.UserId,
                    request.Reason,
                    cancellationToken);

                if (result)
                {
                    _logger.LogInformation(
                        "Report revoked: {ReportId} by {UserId}. Reason: {Reason}",
                        request.ReportId, _currentUserService.UserId, request.Reason);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke report: {ReportId}", request.ReportId);
                throw;
            }
        }
    }
}
