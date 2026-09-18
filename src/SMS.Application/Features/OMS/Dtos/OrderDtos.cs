using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Application.Features.OMS.Dtos
{
    /// <summary>Read model for an OMS order.</summary>
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusName => Status.ToString();

        public string RequestedByUserId { get; set; } = string.Empty;
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public string? ApprovalRemarks { get; set; }
        public string? RejectedByUserId { get; set; }
        public DateTime? RejectedAtUtc { get; set; }
        public string? RejectionRemarks { get; set; }
        public string? CancelledByUserId { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public DateTime? RequiredByDate { get; set; }

        public Guid? RoadAccountId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int ItemCount { get; set; }
        public IReadOnlyList<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();

        public static OrderDto FromEntity(Order order, bool includeItems = true)
        {
            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Title = order.Title,
                Description = order.Description,
                Status = order.Status,
                RequestedByUserId = order.RequestedByUserId,
                ApprovedByUserId = order.ApprovedByUserId,
                ApprovedAtUtc = order.ApprovedAtUtc,
                ApprovalRemarks = order.ApprovalRemarks,
                RejectedByUserId = order.RejectedByUserId,
                RejectedAtUtc = order.RejectedAtUtc,
                RejectionRemarks = order.RejectionRemarks,
                CancelledByUserId = order.CancelledByUserId,
                CancelledAtUtc = order.CancelledAtUtc,
                CancellationReason = order.CancellationReason,
                SubmittedAtUtc = order.SubmittedAtUtc,
                RequiredByDate = order.RequiredByDate,
                RoadAccountId = order.RoadAccountId,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                ItemCount = order.Items.Count,
                Items = includeItems
                    ? order.Items.OrderBy(i => i.RowNumber).Select(OrderItemDto.FromEntity).ToList()
                    : Array.Empty<OrderItemDto>()
            };
        }
    }

    /// <summary>Read model for an OMS order line item.</summary>
    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public int RowNumber { get; set; }
        public string? ItemCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public static OrderItemDto FromEntity(OrderItem item) => new()
        {
            Id = item.Id,
            OrderId = item.OrderId,
            RowNumber = item.RowNumber,
            ItemCode = item.ItemCode,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.LineTotal
        };
    }
}