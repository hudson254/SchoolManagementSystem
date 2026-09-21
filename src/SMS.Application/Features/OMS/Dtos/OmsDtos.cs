using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Application.Features.OMS.Dtos
{
    /// <summary>Read model for an append-only order status history entry.</summary>
    public class OrderStatusHistoryDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public OrderStatus? FromStatus { get; set; }
        public OrderStatus ToStatus { get; set; }
        public OrderActionType Action { get; set; }
        public string PerformedByUserId { get; set; } = string.Empty;
        public string? PerformedByUsername { get; set; }
        public DateTime PerformedAtUtc { get; set; }
        public string? Remarks { get; set; }

        public static OrderStatusHistoryDto FromEntity(OrderStatusHistory h) => new()
        {
            Id = h.Id,
            OrderId = h.OrderId,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            Action = h.Action,
            PerformedByUserId = h.PerformedByUserId,
            PerformedByUsername = h.PerformedByUsername,
            PerformedAtUtc = h.PerformedAtUtc,
            Remarks = h.Remarks
        };
    }

    /// <summary>Read model for a road account (no accounting math — open requirement #18).</summary>
    public class RoadAccountDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public RoadAccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public static RoadAccountDto FromEntity(RoadAccount a) => new()
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            AccountType = a.AccountType,
            Balance = a.Balance,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt
        };
    }

    /// <summary>Tenant-scoped OMS dashboard summary (status counters + totals).</summary>
    public class OmsDashboardSummaryDto
    {
        public int DraftCount { get; set; }
        public int SubmittedCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }
        public int TotalOrders { get; set; }

        /// <summary>Total order amount grouped by currency for the requested window.</summary>
        public IReadOnlyDictionary<string, decimal> TotalsByCurrency { get; set; } =
            new Dictionary<string, decimal>();
    }
}