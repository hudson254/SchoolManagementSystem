namespace SMS.Application.Common
{
    /// <summary>
    /// Result of a report export operation
    /// </summary>
    public class ReportFileResult
    {
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
