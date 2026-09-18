using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System.Linq;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Lifecycle / state-machine tests (Phase 2).
    /// Verifies the development-default state machine documented in
    /// Documentation/OMS/OMS_ARCHITECTURE.md §6 and
    /// OMS_OPEN_REQUIREMENTS.md #2.
    /// </summary>
    public class OrderLifecycleTests
    {
        private static Order CreateDraft(string creatorId = "creator-id", Guid? tenantId = null)
        {
            var order = Order.Create(
                OmsOrderNumber.Format(DateTime.UtcNow.Year, 1),
                "Test Order",
                null,
                creatorId,
                OmsMoney.DefaultCurrency,
                null,
                null);
            order.TenantId = tenantId ?? Guid.NewGuid();

            // Submitting requires at least one item (open requirement #8), so the
            // helper seeds one item to allow lifecycle transitions in tests.
            order.AddItem(null, "Test Item", 1, 10m);
            return order;
        }

        [Fact]
        public void Draft_To_Submitted_IsAllowed()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.Status.Should().Be(OrderStatus.Submitted);
        }

        [Fact]
        public void Submitted_To_PendingApproval_IsAllowed()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.StartReview("reviewer-id", "Reviewer");
            order.Status.Should().Be(OrderStatus.PendingApproval);
        }

        [Theory]
        [InlineData(OrderStatus.Approved)]
        [InlineData(OrderStatus.Rejected)]
        [InlineData(OrderStatus.Cancelled)]
        public void CanTransition_FromTerminalStatus_IsFalse(OrderStatus terminal)
        {
            OrderLifecycle.CanTransition(terminal, OrderStatus.Draft).Should().BeFalse();
            OrderLifecycle.CanTransition(terminal, OrderStatus.Approved).Should().BeFalse();
        }

        [Fact]
        public void Approved_To_Cancelled_IsNotAllowed()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.Approve("approver-id", "Approver", null);
            order.Invoking(o => o.Cancel("user", "User", "changed mind"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*Invalid order status transition*");
        }

        [Fact]
        public void Submitted_To_Cancelled_IsAllowed()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.Cancel("creator-id", "Creator", "no longer needed");
            order.Status.Should().Be(OrderStatus.Cancelled);
        }

        [Fact]
        public void SameStatusTransition_IsNotAllowed()
        {
            OrderLifecycle.CanTransition(OrderStatus.Draft, OrderStatus.Draft).Should().BeFalse();
            OrderLifecycle.CanTransition(OrderStatus.Approved, OrderStatus.Approved).Should().BeFalse();
        }

        [Fact]
        public void IsTerminal_MarksFinalStatuses()
        {
            OrderLifecycle.IsTerminal(OrderStatus.Approved).Should().BeTrue();
            OrderLifecycle.IsTerminal(OrderStatus.Rejected).Should().BeTrue();
            OrderLifecycle.IsTerminal(OrderStatus.Cancelled).Should().BeTrue();
            OrderLifecycle.IsTerminal(OrderStatus.Draft).Should().BeFalse();
        }

        [Theory]
        [InlineData(OrderStatus.Draft, OrderStatus.Submitted, true)]
        [InlineData(OrderStatus.Draft, OrderStatus.Cancelled, true)]
        [InlineData(OrderStatus.Submitted, OrderStatus.PendingApproval, true)]
        [InlineData(OrderStatus.Submitted, OrderStatus.Approved, true)]
        [InlineData(OrderStatus.Submitted, OrderStatus.Rejected, true)]
        [InlineData(OrderStatus.Submitted, OrderStatus.Cancelled, true)]
        [InlineData(OrderStatus.PendingApproval, OrderStatus.Approved, true)]
        [InlineData(OrderStatus.PendingApproval, OrderStatus.Rejected, true)]
        [InlineData(OrderStatus.PendingApproval, OrderStatus.Cancelled, true)]
        [InlineData(OrderStatus.Draft, OrderStatus.PendingApproval, false)]
        [InlineData(OrderStatus.Draft, OrderStatus.Approved, false)]
        [InlineData(OrderStatus.Submitted, OrderStatus.Draft, false)]
        public void CanTransition_CoversStateMachine(OrderStatus from, OrderStatus to, bool expected)
        {
            OrderLifecycle.CanTransition(from, to).Should().Be(expected);
        }

        [Fact]
        public void EveryTransition_RecordsHistoryEntry()
        {
            var order = CreateDraft();
            order.Submit("creator-id", "Creator");
            order.StartReview("reviewer-id", "Reviewer");
            order.Approve("approver-id", "Approver", "ok");

            order.StatusHistory.Should().HaveCount(3);
            order.StatusHistory.Select(h => h.ToStatus)
                .Should().ContainInOrder(OrderStatus.Submitted, OrderStatus.PendingApproval, OrderStatus.Approved);
        }
    }
}
