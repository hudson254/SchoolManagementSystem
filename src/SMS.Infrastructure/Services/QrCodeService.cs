using Microsoft.Extensions.Logging;
using QRCoder;
using SMS.Domain.Interfaces;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Service for generating QR codes for report verification and certificates.
    /// </summary>
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
