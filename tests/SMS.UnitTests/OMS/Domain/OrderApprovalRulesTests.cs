using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Order approval ownership rules (Phase 2). Per open requirement #5,
    /// the creator of an order must not approve it. Authorization (role checks)
    /// lives at the application/policy layer, not in the domain entity.
    /// </summary>
    public class OrderApprovalRulesTests
    {
        private static Order CreateApprovedOrder(string creatorId = "creator-id")
        {
            var order = Order.Create("ORD-2026-000001", "title", null, creatorId, "KES", null, null);
            order.TenantId = Guid.NewGuid();

            // Submitting requires at least one item (open requirement #8).
            order.AddItem(null, "Test Item", 1, 10m);
            order.Submit(creatorId, "Creator");
            return order;
        }

        [Fact]
        public void Creator_CannotApproveOwnOrder()
        {
            var order = CreateApprovedOrder("creator-id");
            order.Invoking(o => o.Approve("creator-id", "Creator", null))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*creator*cannot*approve*");
        }

        [Fact]
        public void Creator_CannotApproveOwnOrder_EvenViaReviewPath()
        {
            var order = CreateApprovedOrder("creator-id");
            order.StartReview("creator-id", "Creator"); // creator acting as reviewer
            order.Invoking(o => o.Approve("creator-id", "Creator", null))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*creator*cannot*approve*");
        }

        [Fact]
        public void DifferentUser_CanApprove()
        {
            var order = CreateApprovedOrder("creator-id");
            order.Approve("approver-id", "Approver", "approved");

            order.Status.ToString().Should().Be("Approved");
            order.ApprovedByUserId.Should().Be("approver-id");
            order.ApprovedAtUtc.Should().NotBeNull();
            order.ApprovalRemarks.Should().Be("approved");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Approve_WithEmptyPerformer_Throws(string? badUser)
        {
            var order = CreateApprovedOrder("creator-id");
            order.Invoking(o => o.Approve(badUser!, "Approver", null))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Approve_AppendsStatusHistory()
        {
            var order = CreateApprovedOrder("creator-id");
            order.Approve("approver-id", "Approver", null);

            order.StatusHistory.Should().Contain(h => h.ToStatus == OrderStatus.Approved);
            var last = order.StatusHistory.Last();
            last.PerformedByUserId.Should().Be("approver-id");
            last.Action.ToString().Should().Be("Approved");
            last.FromStatus.Should().Be(OrderStatus.Submitted);
        }

        [Fact]
        public void Reject_AppendsStatusHistory()
        {
            var order = CreateApprovedOrder("creator-id");
            order.Reject("approver-id", "Approver", "budget issue");

            order.Status.ToString().Should().Be("Rejected");
            order.StatusHistory.Should().Contain(h => h.ToStatus == OrderStatus.Rejected);
            order.RejectionRemarks.Should().Be("budget issue");
        }

        [Fact]
        public void Reject_WithoutRemarks_Throws()
        {
            var order = CreateApprovedOrder("creator-id");
            order.Invoking(o => o.Reject("approver-id", "Approver", ""))
                .Should().Throw<ArgumentException>()
                .WithMessage("*remark*");
        }
    }
}
