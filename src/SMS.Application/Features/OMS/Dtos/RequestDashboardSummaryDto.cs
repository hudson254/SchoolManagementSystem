using SMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace SMS.Application.Features.OMS.Dtos
{
    public class RequestDashboardSummaryDto
    {
        public int TotalRequests { get; set; }
        public int DraftCount { get; set; }
        public int SubmittedCount { get; set; }
        public int PendingReviewCount { get; set; }
        public int AssignedCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int ReturnedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int OnHoldCount { get; set; }
        public int EscalatedCount { get; set; }
        public IReadOnlyDictionary<RequestStatus, int> CountsByStatus { get; set; } = new Dictionary<RequestStatus, int>();
    }

    public class PagedRequestResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}