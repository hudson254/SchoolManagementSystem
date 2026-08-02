using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.ReportVerification.Queries
{
    /// <summary>
    /// Query to search and filter report verification records.
    /// Requires administrative authorization.
    /// </summary>
    public class SearchReportsQuery : IRequest<ReportVerificationListDto>
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

    public class SearchReportsHandler : IRequestHandler<SearchReportsQuery, ReportVerificationListDto>
    {
        private readonly IReportVerificationRepository _repository;
        private readonly ILogger<SearchReportsHandler> _logger;

        public SearchReportsHandler(
            IReportVerificationRepository repository,
            ILogger<SearchReportsHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ReportVerificationListDto> Handle(SearchReportsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var (records, totalCount) = await _repository.GetFilteredAsync(
                    reportType: request.ReportType,
                    reportId: request.ReportId,
                    generatedBy: request.GeneratedBy,
                    status: request.Status,
                    startDate: request.StartDate,
                    endDate: request.EndDate,
                    page: request.Page,
                    pageSize: request.PageSize,
                    cancellationToken: cancellationToken);

                var dtoRecords = records.Select(r => new ReportVerificationRecordDto
                {
                    Id = r.Id,
                    ReportId = r.ReportId,
                    ReportType = r.ReportType,
                    ReportName = r.ReportName,
                    GeneratedByUserName = r.GeneratedByUserName,
                    GeneratedDate = r.GeneratedDate,
                    Status = r.Status.ToString(),
                    VerificationCount = r.VerificationCount,
                    LastVerified = r.LastVerified,
                    RevokedDate = r.RevokedDate,
                    RevokedBy = r.RevokedBy,
                    RevocationReason = r.RevocationReason,
                    Version = r.Version,
                    ExpirationDate = r.ExpirationDate
                }).ToList();

                _logger.LogInformation(
                    "Report verification search returned {Count} results (page {Page}/{TotalPages})",
                    dtoRecords.Count, request.Page, (int)Math.Ceiling((double)totalCount / request.PageSize));

                return new ReportVerificationListDto
                {
                    Records = dtoRecords,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching report verification records");
                throw;
            }
        }
    }
}
