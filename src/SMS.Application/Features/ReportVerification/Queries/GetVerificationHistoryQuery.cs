using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.ReportVerification.Queries
{
    /// <summary>
    /// Query to get verification history for reports.
    /// Uses audit logs to retrieve verification history.
    /// </summary>
    public class GetVerificationHistoryQuery : IRequest<List<VerificationHistoryEntryDto>>
    {
        public string? ReportId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetVerificationHistoryHandler : IRequestHandler<GetVerificationHistoryQuery, List<VerificationHistoryEntryDto>>
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<GetVerificationHistoryHandler> _logger;

        public GetVerificationHistoryHandler(
            IAuditService auditService,
            ILogger<GetVerificationHistoryHandler> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<List<VerificationHistoryEntryDto>> Handle(GetVerificationHistoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var (logs, _) = await _auditService.GetAuditLogsAsync(
                    action: "ReportVerified",
                    entityName: request.ReportId != null ? "ReportVerification" : null,
                    page: request.Page,
                    pageSize: request.PageSize);

                var entries = new List<VerificationHistoryEntryDto>();
                foreach (var log in logs)
                {
                    entries.Add(new VerificationHistoryEntryDto
                    {
                        Timestamp = log.Timestamp,
                        User = log.Username ?? "Unknown",
                        IpAddress = log.IPAddress ?? "Unknown",
                        Result = log.Success ? "Success" : "Failed"
                    });
                }

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verification history");
                return new List<VerificationHistoryEntryDto>();
            }
        }
    }
}
