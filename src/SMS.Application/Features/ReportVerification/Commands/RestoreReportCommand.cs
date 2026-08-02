using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.ReportVerification.Commands
{
    /// <summary>
    /// Command to restore a previously revoked report.
    /// </summary>
    public class RestoreReportCommand : IRequest<bool>
    {
        public string ReportId { get; set; } = string.Empty;
    }

    public class RestoreReportHandler : IRequestHandler<RestoreReportCommand, bool>
    {
        private readonly IReportAuthenticationService _authService;
        private readonly SMS.Domain.Interfaces.ICurrentUserService _currentUserService;
        private readonly ILogger<RestoreReportHandler> _logger;

        public RestoreReportHandler(
            IReportAuthenticationService authService,
            SMS.Domain.Interfaces.ICurrentUserService currentUserService,
            ILogger<RestoreReportHandler> logger)
        {
            _authService = authService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(RestoreReportCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _authService.RestoreReportAsync(
                    request.ReportId,
                    _currentUserService.UserId,
                    cancellationToken);

                if (result)
                {
                    _logger.LogInformation(
                        "Report restored: {ReportId} by {UserId}",
                        request.ReportId, _currentUserService.UserId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore report: {ReportId}", request.ReportId);
                throw;
            }
        }
    }
}
