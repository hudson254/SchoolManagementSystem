using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;

namespace SMS.Application.Features.OMS.Dtos
{
    public class RequestListItemDto
    {
        public Guid Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public RequestPriority Priority { get; set; }
        public RequestStatus Status { get; set; }
        public string RequesterUserId { get; set; } = string.Empty;
        public string? AssignedUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public static RequestListItemDto FromEntity(Request r) => new()
        {
            Id = r.Id,
            RequestNumber = r.RequestNumber,
            RequestType = r.RequestType,
            Title = r.Title,
            Priority = r.Priority,
            Status = r.Status,
            RequesterUserId = r.RequesterUserId,
            AssignedUserId = r.AssignedUserId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            DueDate = r.DueDate
        };
    }
}