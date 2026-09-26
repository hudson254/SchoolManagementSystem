using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;

namespace SMS.Application.Features.OMS.Dtos
{
    public class RequestStatusHistoryDto
    {
        public Guid Id { get; set; }
        public Guid RequestId { get; set; }
        public RequestStatus? FromStatus { get; set; }
        public RequestStatus ToStatus { get; set; }
        public RequestActionType Action { get; set; }
        public string PerformedByUserId { get; set; } = string.Empty;
        public string? PerformedByUsername { get; set; }
        public DateTime PerformedAtUtc { get; set; }
        public string? Reason { get; set; }

        public static RequestStatusHistoryDto FromEntity(RequestStatusHistory h) => new()
        {
            Id = h.Id,
            RequestId = h.RequestId,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            Action = h.Action,
            PerformedByUserId = h.PerformedByUserId,
            PerformedByUsername = h.PerformedByUsername,
            PerformedAtUtc = h.PerformedAtUtc,
            Reason = h.Reason
        };
    }
}