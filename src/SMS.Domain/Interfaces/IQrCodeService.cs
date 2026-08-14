namespace SMS.Domain.Interfaces;

/// <summary>
/// Service for generating QR codes for certificates and report verification.
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generates a QR code as a PNG byte array.
    /// </summary>
    /// <param name="content">Content to encode in the QR code</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels</param>
    /// <returns>PNG byte array</returns>
    Task<byte[]> GenerateQrCodeAsync(string content, int pixelsPerModule = 10);
}
