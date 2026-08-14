using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Centralized enterprise-grade upload service.
    /// All file uploads across the system MUST go through this service.
    /// </summary>
    public interface IUploadService
    {
        /// <summary>
        /// Uploads a file with full validation, scanning, naming, and metadata tracking.
        /// </summary>
        /// <param name="fileStream">The file content stream</param>
        /// <param name="originalFileName">Original filename from the user</param>
        /// <param name="category">Upload category for validation rules</param>
        /// <param name="userId">ID of the uploading user</param>
        /// <param name="username">Username of the uploader</param>
        /// <param name="context">Optional upload context (course offering, unit, assignment, etc.)</param>
        /// <returns>Upload result with generated filename, path, and metadata</returns>
        Task<UploadResult> UploadAsync(
            Stream fileStream,
            string originalFileName,
            UploadCategory category,
            string userId,
            string username,
            UploadContext? context = null);

        /// <summary>
        /// Downloads a file by its ID.
        /// </summary>
        Task<Stream> DownloadAsync(Guid uploadFileId);

        /// <summary>
        /// Downloads a file by its storage path.
        /// </summary>
        Task<Stream> DownloadByPathAsync(string storagePath);

        /// <summary>
        /// Deletes a file (soft delete - marks as deleted).
        /// </summary>
        Task<bool> DeleteAsync(Guid uploadFileId, string deletedBy);

        /// <summary>
        /// Permanently deletes a file from storage and database.
        /// </summary>
        Task<bool> PermanentDeleteAsync(Guid uploadFileId);

        /// <summary>
        /// Gets file metadata by ID.
        /// </summary>
        Task<UploadFile> GetMetadataAsync(Guid uploadFileId);

        /// <summary>
        /// Gets file metadata by SHA-256 hash.
        /// </summary>
        Task<UploadFile> GetMetadataByHashAsync(string sha256Hash);

        /// <summary>
        /// Gets the URL for accessing a file.
        /// </summary>
        Task<string> GetFileUrlAsync(Guid uploadFileId);

        /// <summary>
        /// Validates a file without uploading it.
        /// </summary>
        Task<UploadValidationResult> ValidateAsync(
            Stream fileStream,
            string originalFileName,
            UploadCategory category);

        /// <summary>
        /// Checks if a file with the same SHA-256 hash already exists.
        /// </summary>
        Task<bool> IsDuplicateAsync(Stream fileStream);

        /// <summary>
        /// Gets the next version number for a given base identifier.
        /// </summary>
        Task<int> GetNextVersionAsync(string baseIdentifier);
    }

    /// <summary>
    /// Result of a successful upload operation.
    /// </summary>
    public class UploadResult
    {
        public Guid FileId { get; set; }
        public string GeneratedFileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Sha256Hash { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public bool IsDuplicate { get; set; }
        public string? FileUrl { get; set; }
        public string Status { get; set; } = "Active";
    }

    /// <summary>
    /// Result of a file validation check (without upload).
    /// </summary>
    public class UploadValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public string? DetectedMimeType { get; set; }
        public string? DetectedExtension { get; set; }
        public long FileSizeBytes { get; set; }
        public string? Sha256Hash { get; set; }
        public bool IsDuplicate { get; set; }
    }

    /// <summary>
    /// Context information for an upload operation.
    /// </summary>
    public class UploadContext
    {
        public Guid? CourseOfferingId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? AssignmentId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? LecturerId { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// Course offering identifier for naming (e.g., "diplomaict2026s1")
        /// </summary>
        public string? CourseOfferingIdentifier { get; set; }

        /// <summary>
        /// Unit code for naming (e.g., "it201")
        /// </summary>
        public string? UnitCode { get; set; }

        /// <summary>
        /// Unit name for naming (e.g., "database_systems")
        /// </summary>
        public string? UnitName { get; set; }

        /// <summary>
        /// Document type for supporting documents (e.g., "medical_letter")
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// Department name for admin documents (e.g., "registrar")
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Purpose description for admin documents (e.g., "student_clearance")
        /// </summary>
        public string? Purpose { get; set; }

        /// <summary>
        /// Date string for admin documents (e.g., "20260807")
        /// </summary>
        public string? DateString { get; set; }
    }
}
