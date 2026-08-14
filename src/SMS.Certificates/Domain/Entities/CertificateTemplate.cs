namespace SMS.Certificates.Domain.Entities;

/// <summary>
/// Represents a certificate template
/// </summary>
public class CertificateTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Template version
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Type of certificate this template is for
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Template status (Active/Inactive)
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Course ID this template applies to (null for all courses)
    /// </summary>
    public Guid? CourseId { get; set; }

    /// <summary>
    /// Path to the template PDF file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the institution logo image
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Path to the watermark image
    /// </summary>
    public string? WatermarkPath { get; set; }

    /// <summary>
    /// JSON configuration for field mappings (positions, fonts, etc.)
    /// </summary>
    public string FieldMappings { get; set; } = "{}";

    /// <summary>
    /// Whether this is the default template
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Timestamp when the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the template
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the template was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who last updated the template
    /// </summary>
    public Guid UpdatedBy { get; set; }
}
