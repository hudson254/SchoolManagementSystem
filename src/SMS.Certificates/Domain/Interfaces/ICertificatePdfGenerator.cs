namespace SMS.Certificates.Domain.Interfaces;

/// <summary>
/// Service for generating certificate PDFs
/// </summary>
public interface ICertificatePdfGenerator
{
    /// <summary>
    /// Generate a certificate PDF
    /// </summary>
    /// <param name="certificate">Certificate data</param>
    /// <param name="template">Template configuration</param>
    /// <param name="qrCodePath">Path to QR code image</param>
    /// <returns>PDF file path</returns>
    Task<string> GenerateCertificatePdfAsync(
        CertificateGenerationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for certificate PDF generation
/// </summary>
public class CertificateGenerationRequest
{
    /// <summary>
    /// Certificate number
    /// </summary>
    public string CertificateNumber { get; set; } = string.Empty;

    /// <summary>
    /// Student full name
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Course offering
    /// </summary>
    public string CourseOffering { get; set; } = string.Empty;

    /// <summary>
    /// Course code
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Completion date
    /// </summary>
    public DateTime CompletionDate { get; set; }

    /// <summary>
    /// Course duration
    /// </summary>
    public string CourseDuration { get; set; } = string.Empty;

    /// <summary>
    /// Final grade
    /// </summary>
    public string? FinalGrade { get; set; }

    /// <summary>
    /// Classification
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Date awarded
    /// </summary>
    public DateTime DateAwarded { get; set; }

    /// <summary>
    /// Institution name
    /// </summary>
    public string Institution { get; set; } = string.Empty;

    /// <summary>
    /// Path to template PDF
    /// </summary>
    public string TemplatePdfPath { get; set; } = string.Empty;

    /// <summary>
    /// Field mappings JSON
    /// </summary>
    public string FieldMappings { get; set; } = "{}";

    /// <summary>
    /// Path to QR code image
    /// </summary>
    public string? QrCodePath { get; set; }

    /// <summary>
    /// Path to institution logo
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Path to watermark image
    /// </summary>
    public string? WatermarkPath { get; set; }

    /// <summary>
    /// Digital signatures to apply
    /// </summary>
    public List<DigitalSignatureRequest> Signatures { get; set; } = new();
}

/// <summary>
/// Digital signature request
/// </summary>
public class DigitalSignatureRequest
{
    /// <summary>
    /// Signature name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Signature type/role
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Path to signature image
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y coordinate
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Width
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Height
    /// </summary>
    public float Height { get; set; }
}
