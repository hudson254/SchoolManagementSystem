using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Service for generating QR codes for report verification.
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

    public class QrCodeService : IQrCodeService
    {
        private readonly ILogger<QrCodeService> _logger;

        public QrCodeService(ILogger<QrCodeService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateQrCodeAsync(string content, int pixelsPerModule = 10)
        {
            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
                        _logger.LogDebug("QR code generated successfully for content length {Length}", content.Length);
                        return await Task.FromResult(qrCodeBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate QR code");
                throw;
            }
        }
    }
}
