namespace SMS.Certificates.Domain.Entities;

/// <summary>
/// Represents a digital signature that can be placed on certificates
/// </summary>
public class DigitalSignature
{
    /// <summary>
    /// Unique identifier for the signature
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the signatory (e.g., "John Smith")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type/role of the signatory (e.g., Principal, Director, Coordinator, Registrar)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Path to the signature image file
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this signature is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Status of the signature (Active/Inactive) - used by DbContext configuration
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Timestamp when the signature was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the signature
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the signature was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who last updated the signature
    /// </summary>
    public Guid UpdatedBy { get; set; }
}
