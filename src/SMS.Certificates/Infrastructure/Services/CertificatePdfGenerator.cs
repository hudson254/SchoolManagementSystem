using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMS.Certificates.Domain.Entities;
using SMS.Certificates.Domain.Interfaces;
using SMS.Domain.Interfaces;

namespace SMS.Certificates.Infrastructure.Services;

/// <summary>
/// Implementation of certificate PDF generator using QuestPDF
/// </summary>
public class CertificatePdfGenerator : ICertificatePdfGenerator
{
    private readonly ILogger<CertificatePdfGenerator> _logger;
    private readonly IQrCodeService _qrCodeService;
    private readonly IFileStorageService _fileStorageService;

    public CertificatePdfGenerator(
        ILogger<CertificatePdfGenerator> logger,
        IQrCodeService qrCodeService,
        IFileStorageService fileStorageService)
    {
        _logger = logger;
        _qrCodeService = qrCodeService;
        _fileStorageService = fileStorageService;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateCertificatePdfAsync(
        CertificateGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.CertificateNumber))
                throw new ArgumentException("Certificate number is required", nameof(request.CertificateNumber));

            if (string.IsNullOrWhiteSpace(request.StudentName))
                throw new ArgumentException("Student name is required", nameof(request.StudentName));

            if (string.IsNullOrWhiteSpace(request.CourseName))
                throw new ArgumentException("Course name is required", nameof(request.CourseName));

            // Generate QR code if not provided
            string? qrCodePath = request.QrCodePath;
            if (string.IsNullOrEmpty(qrCodePath))
            {
                var verificationUrl = $"https://school.edu/verify?token={request.CertificateNumber}";
                var qrBytes = await _qrCodeService.GenerateQrCodeAsync(verificationUrl, 10);
                qrCodePath = Path.Combine(
                    Path.GetTempPath(),
                    $"qr_{request.CertificateNumber}_{Guid.NewGuid():N}.png");
                await File.WriteAllBytesAsync(qrCodePath, qrBytes, cancellationToken);
            }

            // Generate PDF using QuestPDF
            var outputPath = Path.Combine(
                Path.GetTempPath(),
                $"Certificate_{request.CertificateNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");

            // Create PDF document
            var document = CreateCertificateDocument(request, qrCodePath);

            // Save to file
            document.GeneratePdf(outputPath);

            // Upload to file storage if configured
            if (_fileStorageService != null)
            {
                try
                {
                    var pdfBytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
                    var storedPath = await _fileStorageService.SaveFileAsync(
                        pdfBytes,
                        $"certificate_{request.CertificateNumber}.pdf",
                        "certificates");
                    _logger.LogInformation("Certificate PDF uploaded to storage: {Path}", storedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload certificate PDF to storage for {CertificateNumber}", request.CertificateNumber);
                }
            }

            // Clean up temporary QR code if we generated it
            if (string.IsNullOrEmpty(request.QrCodePath) && !string.IsNullOrEmpty(qrCodePath))
            {
                try
                {
                    if (File.Exists(qrCodePath))
                        File.Delete(qrCodePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            _logger.LogInformation("Generated certificate PDF for {CertificateNumber} at {Path}",
                request.CertificateNumber, outputPath);

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate PDF for {CertificateNumber}", request.CertificateNumber);
            throw;
        }
    }

    private IDocument CreateCertificateDocument(
        CertificateGenerationRequest request,
        string qrCodePath)
    {
        // Parse field mappings
        var fieldMappings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, FieldMapping>>(
            request.FieldMappings) ?? new Dictionary<string, FieldMapping>();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);

                // Add watermark if provided
                if (!string.IsNullOrEmpty(request.WatermarkPath))
                {
                    page.Header().Element(container => ComposeWatermark(container, request.WatermarkPath));
                }

                // Add institution logo if provided
                if (!string.IsNullOrEmpty(request.LogoPath))
                {
                    page.Header().Element(container => ComposeLogo(container, request.LogoPath));
                }

                // Main content
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    column.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION")
                        .FontSize(32)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    column.Item().AlignCenter().Text("This is to certify that")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);

                    // Student Name
                    column.Item().AlignCenter().PaddingTop(0.5f, Unit.Centimetre)
                        .Text(request.StudentName)
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Black);

                    column.Item().AlignCenter().Text("has successfully completed the course")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken2);

                    // Course Name
                    column.Item().AlignCenter().PaddingTop(0.3f, Unit.Centimetre)
                        .Text(request.CourseName)
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    // Course details
                    column.Item().PaddingTop(1, Unit.Centimetre).Column(detailsColumn =>
                    {
                        detailsColumn.Item().Row(row =>
                        {
                            row.AutoItem().Text("Course Code:").FontSize(12).Bold();
                            row.AutoItem().Text(request.CourseCode).FontSize(12);
                        });

                        detailsColumn.Item().Row(row =>
                        {
                            row.AutoItem().Text("Course Offering:").FontSize(12).Bold();
                            row.AutoItem().Text(request.CourseOffering).FontSize(12);
                        });

                        detailsColumn.Item().Row(row =>
                        {
                            row.AutoItem().Text("Completion Date:").FontSize(12).Bold();
                            row.AutoItem().Text(request.CompletionDate.ToString("dd MMMM yyyy")).FontSize(12);
                        });

                        if (!string.IsNullOrEmpty(request.FinalGrade))
                        {
                            detailsColumn.Item().Row(row =>
                            {
                                row.AutoItem().Text("Final Grade:").FontSize(12).Bold();
                                row.AutoItem().Text(request.FinalGrade).FontSize(12);
                            });
                        }

                        if (!string.IsNullOrEmpty(request.Classification))
                        {
                            detailsColumn.Item().Row(row =>
                            {
                                row.AutoItem().Text("Classification:").FontSize(12).Bold();
                                row.AutoItem().Text(request.Classification).FontSize(12);
                            });
                        }
                    });

                    // Certificate Number and QR Code
                    column.Item().PaddingTop(1, Unit.Centimetre).Row(row =>
                    {
                        row.RelativeItem().Column(certColumn =>
                        {
                            certColumn.Item().Text("Certificate Number:").FontSize(10).Bold();
                            certColumn.Item().Text(request.CertificateNumber).FontSize(11);
                            certColumn.Item().Text($"Issued: {request.DateAwarded:dd MMMM yyyy}").FontSize(10);
                        });

                        row.AutoItem().Width(100).Height(100).Image(qrCodePath);
                    });
                });

                // Digital Signatures
                if (request.Signatures.Any())
                {
                    page.Footer().PaddingTop(1, Unit.Centimetre).Column(sigColumn =>
                    {
                        sigColumn.Item().Text("Authorized Signatures").FontSize(12).Bold().AlignCenter();

                        sigColumn.Item().PaddingTop(0.5f, Unit.Centimetre).Row(row =>
                        {
                            foreach (var signature in request.Signatures)
                            {
                                row.AutoItem().Column(sigItem =>
                                {
                                    sigItem.Item().Height(50).Image(signature.ImagePath);
                                    sigItem.Item().Text(signature.Name).FontSize(10).AlignCenter();
                                    sigItem.Item().Text(signature.Type).FontSize(9).AlignCenter().FontColor(Colors.Grey.Darken1);
                                });
                            }
                        });
                    });
                }
            });
        });
    }

    private QuestPDF.Fluent.ImageDescriptor ComposeWatermark(IContainer container, string watermarkPath)
    {
        return container.AlignCenter().Image(watermarkPath)
            .FitWidth()
            .FitHeight();
    }

    private QuestPDF.Fluent.ImageDescriptor ComposeLogo(IContainer container, string logoPath)
    {
        return container.AlignRight().Image(logoPath)
            .FitWidth()
            .FitHeight();
    }

    #region Helper Classes

    private class FieldMapping
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string Font { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;
        public string FontColor { get; set; } = "#000000";
        public string Alignment { get; set; } = "Left";
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public float Rotation { get; set; }
        public float CharacterSpacing { get; set; }
        public float LineSpacing { get; set; }
    }

    #endregion
}
