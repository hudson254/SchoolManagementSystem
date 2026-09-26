using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System.Linq;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Generic SMS request aggregate tests (Phase 2).
    /// Covers the domain-level requirements that the thin module adapters and
    /// controllers rely on: numbering format, lifecycle transitions, terminal
    /// immutability, append-only status history, and comment behaviour.
    /// </summary>
    public class RequestAggregateTests
    {
        private static Request CreateDraft(string requesterId = "requester-id", Guid? tenantId = null)
        {
            var request = Request.Create(
                SmsRequestNumber.Format(DateTime.UtcNow.Year, 1),
                "General",
                "General",
                "Test request",
                "Body",
                requesterId,
                RequestPriority.Normal);
            request.TenantId = tenantId ?? Guid.NewGuid();
            return request;
        }

        [Fact]
        public void Create_SetsDraftStatus_AndPreservesNumber()
        {
            var request = Request.Create("REQ-2026-000042", "General", "General", "Title", null, "requester", RequestPriority.High);

            request.Status.Should().Be(RequestStatus.Draft);
            request.RequestNumber.Should().Be("REQ-2026-000042");
            request.RequesterUserId.Should().Be("requester");
        }

        [Fact]
        public void Submit_FromDraft_MovesToSubmitted()
        {
            var request = CreateDraft();
            request.Submit("requester-id", "Requester");
            request.Status.Should().Be(RequestStatus.Submitted);
        }

        [Theory]
        [InlineData(RequestStatus.Draft, RequestStatus.Submitted, true)]
        [InlineData(RequestStatus.Draft, RequestStatus.Cancelled, true)]
        [InlineData(RequestStatus.Submitted, RequestStatus.PendingReview, true)]
        [InlineData(RequestStatus.Submitted, RequestStatus.Assigned, true)]
        [InlineData(RequestStatus.Submitted, RequestStatus.Rejected, true)]
        [InlineData(RequestStatus.Submitted, RequestStatus.Cancelled, true)]
        [InlineData(RequestStatus.PendingApproval, RequestStatus.Approved, true)]
        [InlineData(RequestStatus.PendingApproval, RequestStatus.Rejected, true)]
        [InlineData(RequestStatus.Draft, RequestStatus.Approved, false)]
        [InlineData(RequestStatus.Draft, RequestStatus.PendingApproval, false)]
        [InlineData(RequestStatus.Submitted, RequestStatus.Draft, false)]
        public void CanTransition_CoversStateMachine(RequestStatus from, RequestStatus to, bool expected)
        {
            RequestLifecycle.CanTransition(from, to).Should().Be(expected);
        }

        [Theory]
        [InlineData(RequestStatus.Completed)]
        [InlineData(RequestStatus.Rejected)]
        [InlineData(RequestStatus.Cancelled)]
        public void CanTransition_FromTerminalStatus_IsFalse(RequestStatus terminal)
        {
            RequestLifecycle.CanTransition(terminal, RequestStatus.Draft).Should().BeFalse();
            RequestLifecycle.CanTransition(terminal, RequestStatus.Approved).Should().BeFalse();
            RequestLifecycle.IsTerminal(terminal).Should().BeTrue();
        }

        [Fact]
        public void IsTerminal_MarksFinalStatuses()
        {
            RequestLifecycle.IsTerminal(RequestStatus.Completed).Should().BeTrue();
            RequestLifecycle.IsTerminal(RequestStatus.Rejected).Should().BeTrue();
            RequestLifecycle.IsTerminal(RequestStatus.Cancelled).Should().BeTrue();
            RequestLifecycle.IsTerminal(RequestStatus.Draft).Should().BeFalse();
        }

        [Fact]
        public void InvalidTransition_ThrowsInvalidOperation()
        {
            var request = CreateDraft();
            request.Invoking(r => r.Approve("approver", "Approver", null))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*Invalid request status transition*");
        }

        [Fact]
        public void CanEdit_AllowsDraftAndReturnedOnly()
        {
            RequestLifecycle.CanEdit(RequestStatus.Draft).Should().BeTrue();
            RequestLifecycle.CanEdit(RequestStatus.Returned).Should().BeTrue();
            RequestLifecycle.CanEdit(RequestStatus.Submitted).Should().BeFalse();
            RequestLifecycle.CanEdit(RequestStatus.Completed).Should().BeFalse();
        }

        [Fact]
        public void EveryTransition_AppendsHistoryEntry()
        {
            var request = CreateDraft();
            request.Submit("requester-id", "Requester");
            request.StartReview("reviewer-id", "Reviewer");

            request.StatusHistory.Should().HaveCount(2);
            request.StatusHistory.Select(h => h.ToStatus)
                .Should().ContainInOrder(RequestStatus.Submitted, RequestStatus.PendingReview);
        }

        [Fact]
        public void RequestNumber_Format_IsTenantSequence()
        {
            SmsRequestNumber.Format(2026, 1).Should().Be("REQ-2026-000001");
            SmsRequestNumber.IsValidFormat("REQ-2026-000001").Should().BeTrue();
            SmsRequestNumber.IsValidFormat("ORD-2026-000001").Should().BeFalse();
        }

        [Fact]
        public void Comment_Create_RequiresAuthorAndMessage()
        {
            var comment = RequestComment.Create(Guid.NewGuid(), "author-id", "hello");
            comment.AuthorUserId.Should().Be("author-id");
            comment.Invoking(_ => RequestComment.Create(Guid.NewGuid(), "", "hello"))
                .Should().Throw<ArgumentException>();
        }
    }
}