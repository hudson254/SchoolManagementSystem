using SMS.Domain.Reporting;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IPdfGenerator
    {
        Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent);
        Task<byte[]> GenerateTranscriptPdfAsync(object transcriptData);
        Task<byte[]> GenerateReportPdfAsync(object reportData);

        /// <summary>
        /// Renders a branded, paginated tabular report (header, applied filters,
        /// summary totals and data table) using the established PDF library.
        /// </summary>
        Task<byte[]> GenerateTablePdfAsync(ReportTableDocument document);
    }
}
