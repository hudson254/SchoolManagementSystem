using SMS.Domain.Entities;
using System;

namespace SMS.Application.Features.OMS.Dtos
{
    public class RequestCommentDto
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public string AuthorUserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAtUtc { get; set; }

        public static RequestCommentDto FromEntity(RequestComment c) => new()
        {
            Id = c.Id,
            RequestId = c.RequestId,
            AuthorUserId = c.AuthorUserId,
            Message = c.Message,
            CreatedAt = c.CreatedAt,
            EditedAtUtc = c.EditedAtUtc
        };
    }
}