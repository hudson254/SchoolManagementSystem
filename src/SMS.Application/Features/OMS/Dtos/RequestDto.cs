using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;

namespace SMS.Application.Features.OMS.Dtos
{
    public class RequestDto
    {
        public Guid Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string TypeDisplayName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public RequestPriority Priority { get; set; }
        public RequestStatus Status { get; set; }
        public string RequesterUserId { get; set; } = string.Empty;
        public string? AssignedUserId { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid TenantId { get; set; }
        public byte[]? RowVersion { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? AccommodationId { get; set; }
        public Guid? AssignmentId { get; set; }
        public Guid? CertificateId { get; set; }

        public static RequestDto FromEntity(Request r) => new()
        {
            Id = r.Id,
            RequestNumber = r.RequestNumber,
            RequestType = r.RequestType,
            TypeDisplayName = r.TypeDisplayName,
            Title = r.Title,
            Description = r.Description,
            Priority = r.Priority,
            Status = r.Status,
            RequesterUserId = r.RequesterUserId,
            AssignedUserId = r.AssignedUserId,
            RelatedEntityId = r.RelatedEntityId,
            RelatedEntityType = r.RelatedEntityType,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            SubmittedAt = r.SubmittedAt,
            CompletedAt = r.CompletedAt,
            DueDate = r.DueDate,
            TenantId = r.TenantId,
            RowVersion = r.RowVersion,
            StudentId = r.StudentId,
            LecturerId = r.LecturerId,
            CourseId = r.CourseId,
            UnitId = r.UnitId,
            EnrollmentId = r.EnrollmentId,
            AccommodationId = r.AccommodationId,
            AssignmentId = r.AssignmentId,
            CertificateId = r.CertificateId
        };
    }
}