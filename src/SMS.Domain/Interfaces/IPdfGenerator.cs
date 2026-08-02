using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IPdfGenerator
    {
        Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent);
        Task<byte[]> GenerateTranscriptPdfAsync(object transcriptData);
        Task<byte[]> GenerateReportPdfAsync(object reportData);
    }
}