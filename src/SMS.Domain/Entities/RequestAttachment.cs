using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities;

/// <summary>
/// Metadata for a file attached to an SMS Request. The file bytes are stored by
/// <see cref="Interfaces.IFileStorageService"/>; this row records the association,
/// tenant, and audit attributes of the stored object.
/// </summary>
public class RequestAttachment : BaseEntity, ITenantAwareEntity
{
    public Guid RequestId { get; set; }

    /// <summary>Original file name supplied by the uploader.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Content type / MIME type of the stored file.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Size of the stored file in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Key/path returned by the file storage service for the stored object.</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>User id of the uploader (string, matching the SMS Request aggregate convention).</summary>
    public string UploadedByUserId { get; set; } = string.Empty;

    /// <summary>Display name of the uploader, captured for audit readability.</summary>
    public string? UploadedByUserName { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>Navigation to the owning request.</summary>
    public Request Request { get; set; } = null!;
}
