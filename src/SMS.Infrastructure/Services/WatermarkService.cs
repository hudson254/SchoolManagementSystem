using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Service for generating watermark images for report authentication.
    /// </summary>
    public interface IWatermarkService
    {
        /// <summary>
        /// Generates a semi-transparent watermark image as PNG byte array.
        /// </summary>
        /// <param name="text">Watermark text</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>PNG byte array</returns>
        Task<byte[]> GenerateWatermarkAsync(string text, int width = 500, int height = 500);
    }

    public class WatermarkService : IWatermarkService
    {
        private readonly ILogger<WatermarkService> _logger;

        public WatermarkService(ILogger<WatermarkService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GenerateWatermarkAsync(string text, int width = 500, int height = 500)
        {
            try
            {
                using (var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)))
                {
                    var canvas = surface.Canvas;

                    // Clear with transparent background
                    canvas.Clear(SKColors.Transparent);

                    using (var paint = new SKPaint
                    {
                        Color = new SKColor(0, 0, 0, 30), // Semi-transparent black (~12% opacity)
                        TextSize = 36,
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        TextAlign = SKTextAlign.Center
                    })
                    {
                        // Draw text centered and rotated -45 degrees for typical watermark look
                        canvas.Save();
                        canvas.Translate(width / 2f, height / 2f);
                        canvas.RotateDegrees(-45);

                        // Draw main text
                        canvas.DrawText(text, 0, 0, paint);

                        // Draw secondary text
                        paint.TextSize = 24;
                        canvas.DrawText("Management Training School", 0, 40, paint);

                        canvas.Restore();
                    }

                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (var ms = new MemoryStream())
                    {
                        data.SaveTo(ms);
                        var bytes = ms.ToArray();
                        _logger.LogDebug("Watermark image generated: {Width}x{Height}", width, height);
                        return await Task.FromResult(bytes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate watermark image");
                // Return empty array instead of throwing to avoid breaking report generation
                return Array.Empty<byte>();
            }
        }
    }
}
