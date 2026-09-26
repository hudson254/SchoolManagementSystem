using SMS.Domain.Entities;
using System;

namespace SMS.Application.Features.OMS.Dtos
{
    /// <summary>
    /// Read model for an OMS request attachment. The physical storage key is
    /// never exposed to clients; downloads must go through the authorized
    /// download endpoint.
    /// </summary>
    public class RequestAttachmentDto
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty;
        public string? UploadedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }

        public static RequestAttachmentDto FromEntity(RequestAttachment a) => new()
        {
            Id = a.Id,
            RequestId = a.RequestId,
            FileName = a.FileName,
            ContentType = a.ContentType,
            Size = a.Size,
            UploadedByUserId = a.UploadedByUserId,
            UploadedByUserName = a.UploadedByUserName,
            CreatedAt = a.CreatedAt
        };
    }

    /// <summary>
    /// Server-side download payload. Returned by the authorized download query
    /// only — never persisted and never exposed through list endpoints.
    /// </summary>
    public class RequestAttachmentDownloadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
