using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Domain.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Accommodation.Queries.Reports
{
    /// <summary>
    /// Exports any accommodation report as PDF or Excel.
    ///
    /// The export re-uses the very same report queries as the on-screen preview
    /// (through MediatR), so the downloaded file always contains exactly the rows
    /// the user is looking at for the same filters — no duplicated query logic.
    /// Row size is bounded by <see cref="MaxRows"/>.
    /// </summary>
    public class ExportAccommodationReportQuery : AccommodationReportQueryBase, IRequest<ReportFileResult>
    {
        /// <summary>
        /// One of: current-occupancy, occupied-houses, empty-houses,
        /// occupancy-history, house-history, occupancy-by-period,
        /// occupant-history, utilization-summary.
        /// </summary>
        public string ReportKey { get; set; } = string.Empty;

        /// <summary>PDF (default) or EXCEL.</summary>
        public string Format { get; set; } = "PDF";

        /// <summary>Occupant to export when <see cref="ReportKey"/> is occupant-history.</summary>
        public Guid? OccupantId { get; set; }

        /// <summary>Optional row cap override; never exceeds <see cref="MaxRows"/>.</summary>
        public int? MaxRows { get; set; }
    }

    public class ExportAccommodationReportHandler : IRequestHandler<ExportAccommodationReportQuery, ReportFileResult>
    {
        /// <summary>Hard cap on rows written to a single export file.</summary>
        public const int MaxRows = 5000;

        private const string PdfContentType = "application/pdf";
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IMediator _mediator;
        private readonly IPdfGenerator _pdfGenerator;
        private readonly IExcelGenerator _excelGenerator;
        private readonly ILogger<ExportAccommodationReportHandler> _logger;

        public ExportAccommodationReportHandler(
            IMediator mediator,
            IPdfGenerator pdfGenerator,
            IExcelGenerator excelGenerator,
            ILogger<ExportAccommodationReportHandler> logger)
        {
            _mediator = mediator;
            _pdfGenerator = pdfGenerator;
            _excelGenerator = excelGenerator;
            _logger = logger;
        }

        public async Task<ReportFileResult> Handle(
            ExportAccommodationReportQuery request,
            CancellationToken cancellationToken)
        {
            request.ValidatePeriod();

            var key = NormalizeReportKey(request.ReportKey);
            var format = NormalizeFormat(request.Format);
            var document = await BuildDocumentAsync(request, key, cancellationToken);

            byte[] content;
            string extension;
            string contentType;

            if (format == "PDF")
            {
                content = await _pdfGenerator.GenerateTablePdfAsync(document);
                extension = "pdf";
                contentType = PdfContentType;
            }
            else
            {
                content = await RenderExcelAsync(document);
                extension = "xlsx";
                contentType = ExcelContentType;
            }

            var fileName = AccommodationReportSupport.BuildFileName(
                $"Accommodation_{FileStem(key)}", request.FromDate, request.ToDate, extension);

            _logger.LogInformation(
                "Accommodation report export: {ReportKey} ({Format}), {Rows} rows, {Bytes} bytes -> {FileName}",
                key, format, document.Rows.Count, content.Length, fileName);

            return new ReportFileResult
            {
                FileContent = content,
                FileName = fileName,
                ContentType = contentType
            };
        }

        // ===== Document building =====

        private async Task<ReportTableDocument> BuildDocumentAsync(
            ExportAccommodationReportQuery request,
            string key,
            CancellationToken cancellationToken)
        {
            switch (key)
            {
                case "current-occupancy":
                    return AccommodationReportTableMapper.FromHouseOccupancy(
                        await _mediator.Send(Seed(request, new GetCurrentOccupancyReportQuery()), cancellationToken));

                case "occupied-houses":
                    return AccommodationReportTableMapper.FromHouseOccupancy(
                        await _mediator.Send(Seed(request, new GetOccupiedHousesReportQuery()), cancellationToken));

                case "empty-houses":
                    return AccommodationReportTableMapper.FromHouseOccupancy(
                        await _mediator.Send(Seed(request, new GetEmptyHousesReportQuery()), cancellationToken));

                case "occupancy-history":
                    return AccommodationReportTableMapper.FromOccupancyHistory(
                        await _mediator.Send(Seed(request, new GetOccupancyHistoryReportQuery()), cancellationToken));

                case "house-history":
                    return AccommodationReportTableMapper.FromHouseHistory(
                        await _mediator.Send(Seed(request, new GetHouseOccupancyHistoryReportQuery()), cancellationToken));

                case "occupancy-by-period":
                    return AccommodationReportTableMapper.FromByPeriod(
                        await _mediator.Send(Seed(request, new GetOccupancyByPeriodReportQuery()), cancellationToken));

                case "occupant-history":
                    return AccommodationReportTableMapper.FromOccupantHistory(
                        await _mediator.Send(
                            Seed(request, new GetOccupantAccommodationHistoryReportQuery { OccupantId = request.OccupantId }),
                            cancellationToken));

                case "utilization-summary":
                    return AccommodationReportTableMapper.FromUtilization(
                        await _mediator.Send(Seed(request, new GetHouseUtilizationSummaryReportQuery()), cancellationToken));

                default:
                    throw new ValidationException($"Unknown accommodation report '{request.ReportKey}'.");
            }
        }

        /// <summary>
        /// Copies the shared filters onto the concrete report query and caps the
        /// export at <see cref="MaxRows"/> so a single file can never grow unbounded.
        /// </summary>
        private static TQuery Seed<TQuery>(ExportAccommodationReportQuery source, TQuery target)
            where TQuery : AccommodationReportQueryBase
        {
            target.LaneId = source.LaneId;
            target.HouseId = source.HouseId;
            target.Status = source.Status;
            target.OccupantType = source.OccupantType;
            target.SemesterId = source.SemesterId;
            target.AcademicYearId = source.AcademicYearId;
            target.FromDate = source.FromDate;
            target.ToDate = source.ToDate;
            target.SearchTerm = source.SearchTerm;
            target.Page = 1;

            var requested = source.MaxRows.GetValueOrDefault(MaxRows);
            target.PageSize = requested < 1 ? MaxRows : Math.Min(requested, MaxRows);

            return target;
        }

        /// <summary>
        /// The shared Excel generator always writes the column header row first, so
        /// the report metadata (generated by/at, summary, applied filters) is
        /// appended below the table as a footer block.
        /// </summary>
        private async Task<byte[]> RenderExcelAsync(ReportTableDocument document)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (var row in document.Rows)
                rows.Add(row);

            rows.Add(Array.Empty<string>());
            rows.Add(new[] { $"Generated on {document.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC by {document.GeneratedBy}" });

            if (document.SummaryLines.Count > 0)
                rows.Add(new[] { string.Join("  |  ", document.SummaryLines) });

            if (document.Filters.Count > 0)
                rows.Add(new[] { string.Join("  |  ", document.Filters.Select(f => $"{f.Label}: {f.Value}")) });

            return await _excelGenerator.GenerateTableExcelAsync(document.ReportTitle, document.Columns, rows);
        }

        // ===== Normalisation =====

        private static string NormalizeReportKey(string? reportKey)
        {
            var key = (reportKey ?? string.Empty).Trim().ToLowerInvariant();
            if (key.Length == 0)
            {
                throw new ValidationException(
                    "A report key is required. Supported values: current-occupancy, occupied-houses, empty-houses, " +
                    "occupancy-history, house-history, occupancy-by-period, occupant-history, utilization-summary.");
            }

            return key;
        }

        private static string NormalizeFormat(string? format)
        {
            switch ((format ?? "PDF").Trim().ToUpperInvariant())
            {
                case "PDF":
                    return "PDF";
                case "EXCEL":
                case "XLSX":
                case "XLS":
                    return "EXCEL";
                default:
                    throw new ValidationException("Unsupported export format. Use PDF or Excel.");
            }
        }

        private static string FileStem(string key) => key switch
        {
            "current-occupancy" => "Current_Occupancy",
            "occupied-houses" => "Occupied_Houses",
            "empty-houses" => "Empty_Houses",
            "occupancy-history" => "Occupancy_History",
            "house-history" => "House_History",
            "occupancy-by-period" => "Occupancy_By_Period",
            "occupant-history" => "Occupant_History",
            "utilization-summary" => "Utilization_Summary",
            _ => "Report"
        };
    }
}
