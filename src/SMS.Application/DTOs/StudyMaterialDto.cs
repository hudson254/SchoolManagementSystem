using System;
using SMS.Domain.Entities;

namespace SMS.Application.DTOs
{
    /// <summary>
    /// Study material (lecture note) metadata for a unit. Downloads are served
    /// through the authenticated download endpoint, never via raw storage paths.
    /// </summary>
    public class StudyMaterialDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid LecturerId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public Guid? UploadFileId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int Version { get; set; } = 1;
        public DateTime UploadedAt { get; set; }
        public bool IsPublished { get; set; } = true;
    }

    public static class StudyMaterialMappings
    {
        public static StudyMaterialDto ToDto(LectureNote note)
        {
            var fileName = !string.IsNullOrEmpty(note.UploadFile?.OriginalFileName)
                ? note.UploadFile.OriginalFileName
                : note.FileName;

            var contentType = !string.IsNullOrEmpty(note.UploadFile?.MimeType)
                ? note.UploadFile.MimeType
                : note.ContentType ?? "application/octet-stream";

            var size = note.UploadFile?.FileSizeBytes ?? note.FileSize;

            return new StudyMaterialDto
            {
                Id = note.Id,
                UnitId = note.UnitId,
                Title = note.Title,
                Description = note.Description,
                LecturerId = note.LecturerId,
                LecturerName = note.Lecturer != null
                    ? $"{note.Lecturer.FirstName} {note.Lecturer.LastName}".Trim()
                    : string.Empty,
                UploadFileId = note.UploadFileId ?? note.UploadFile?.Id,
                OriginalFileName = fileName,
                Extension = note.UploadFile?.Extension ?? GetExtension(fileName),
                ContentType = contentType,
                FileSizeBytes = size,
                Version = note.Version,
                UploadedAt = note.UploadDate,
                IsPublished = note.IsPublished
            };
        }

        private static string GetExtension(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName);
            return string.IsNullOrEmpty(ext) ? string.Empty : ext.ToLowerInvariant();
        }
    }
}