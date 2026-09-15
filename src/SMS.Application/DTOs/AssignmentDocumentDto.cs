using System;
using SMS.Domain.Entities;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Metadata for a question document attached to an assignment. The actual
    /// bytes are stored through the centralized upload pipeline; only metadata
    /// is exposed to the frontend (never internal storage paths).
    /// </summary>
    public class AssignmentDocumentDto
    {
        public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? UploadedByUserId { get; set; }
        public string? UploadedByUsername { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? Description { get; set; }
        public int Version { get; set; } = 1;
        public string Status { get; set; } = "Active";
    }

    public static class AssignmentDocumentMappings
    {
        public static AssignmentDocumentDto ToDto(UploadFile file)
        {
            return new AssignmentDocumentDto
            {
                Id = file.Id,
                AssignmentId = file.AssignmentId ?? Guid.Empty,
                OriginalFileName = file.OriginalFileName,
                Extension = file.Extension,
                MimeType = file.MimeType,
                FileSizeBytes = file.FileSizeBytes,
                UploadedByUserId = file.UploadedByUserId,
                UploadedByUsername = file.UploadedByUsername,
                UploadedAt = file.UploadedAt,
                Description = file.Description,
                Version = file.Version,
                Status = file.Status
            };
        }
    }
}