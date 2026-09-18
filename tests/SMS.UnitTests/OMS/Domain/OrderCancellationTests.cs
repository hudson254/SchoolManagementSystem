using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Order cancellation rules (Phase 2). Per open requirement #11/#12,
    /// cancellation is allowed only from Draft/Submitted/PendingApproval.
    /// A reason is required.
    /// </summary>
    public class OrderCancellationTests
    {
        private static Order CreateDraft(string creatorId = "creator-id")
        {
            var order = Order.Create("ORD-2026-000001", "title", null, creatorId, "KES", null, null);
            order.TenantId = Guid.NewGuid();

            // Submitting requires at least one item (open requirement #8).
            order.AddItem(null, "Test Item", 1, 10m);
            return order;
        }

        [Fact]
        public void Draft_Cancel_Succeeds()
        {
            var order = CreateDraft();
            order.Cancel("creator-id", "Creator", "decided not to proceed");

            order.Status.ToString().Should().Be("Cancelled");
            order.CancelledByUserId.Should().Be("creator-id");
            order.CancellationReason.Should().Be("decided not to proceed");
            order.CancelledAtUtc.Should().NotBeNull();
        }

        [Fact]
        public void Submitted_Cancel_Succeeds()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.Cancel("creator-id", "Creator", "no longer needed");

            order.Status.ToString().Should().Be("Cancelled");
        }

        [Fact]
        public void PendingApproval_Cancel_Succeeds()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.StartReview("reviewer-id", "Reviewer");
            order.Cancel("creator-id", "Creator", "abandoned");

            order.Status.ToString().Should().Be("Cancelled");
        }

        [Fact]
        public void Cancel_WithoutReason_Throws()
        {
            var order = CreateDraft();
            order.Invoking(o => o.Cancel("creator-id", "Creator", ""))
                .Should().Throw<ArgumentException>()
                .WithMessage("*reason*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Cancel_WithBlankReason_Throws(string badReason)
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.Invoking(o => o.Cancel("creator-id", "Creator", badReason))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Approved_Cancel_Throws()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.Approve("approver-id", "Approver", null);
            order.Invoking(o => o.Cancel("creator-id", "Creator", "try"))
                .Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Cancel_AppendsStatusHistory()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");

            order.StatusHistory.Should().Contain(h => h.ToStatus == OrderStatus.Submitted);
        }
    }
}
