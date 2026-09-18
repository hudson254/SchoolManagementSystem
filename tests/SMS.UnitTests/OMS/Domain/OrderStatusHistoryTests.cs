using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// OrderStatusHistory append-only invariant tests (Phase 2).
    /// The history entry is created only via OrderStatusHistory.Create
    /// during a transition; there is no public mutation/edit/delete surface.
    /// </summary>
    public class OrderStatusHistoryTests
    {
        [Fact]
        public void Create_SetsAllFields()
        {
            var history = OrderStatusHistory.Create(
                OrderStatus.Draft,
                OrderStatus.Submitted,
                OrderActionType.Submitted,
                "user-id",
                "User Name",
                "submitted for review");

            history.FromStatus.Should().Be(OrderStatus.Draft);
            history.ToStatus.Should().Be(OrderStatus.Submitted);
            history.Action.Should().Be(OrderActionType.Submitted);
            history.PerformedByUserId.Should().Be("user-id");
            history.PerformedByUsername.Should().Be("User Name");
            history.Remarks.Should().Be("submitted for review");
                                    history.PerformedAtUtc.Should().NotBe(default(DateTime));
        }

        [Fact]
        public void Create_WithEmptyPerformer_Throws()
        {
            var ex = Record.Exception(() =>
                OrderStatusHistory.Create(OrderStatus.Draft, OrderStatus.Submitted,
                    OrderActionType.Submitted, "", null, null));
            ex.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void HistoryEntry_IdsArePrivateSetters()
        {
            // Ensures append-only invariant: callers cannot rewrite history rows.
            // GetSetMethod(nonPublic: false) returns null when there is no PUBLIC setter,
            // which is the real invariant (the private setter exists for EF materialisation).
            var historyType = typeof(OrderStatusHistory);
            historyType.GetProperty(nameof(OrderStatusHistory.FromStatus))!
                .GetSetMethod(nonPublic: false).Should().BeNull();
            historyType.GetProperty(nameof(OrderStatusHistory.ToStatus))!
                .GetSetMethod(nonPublic: false).Should().BeNull();
            historyType.GetProperty(nameof(OrderStatusHistory.PerformedByUserId))!
                .GetSetMethod(nonPublic: false).Should().BeNull();
        }

        [Fact]
        public void Submit_InitialFromStatusIsDraft()
        {
            var order = Order.Create("ORD-2026-000001", "title", null, "u", "KES", null, null);
            order.TenantId = Guid.NewGuid();
            order.AddItem(null, "Widget", 1, 10m); // submit requires >= 1 item (open requirement #8)
            order.Submit("u", "U");

            var history = order.StatusHistory.First();
            history.FromStatus.Should().Be(OrderStatus.Draft);
            history.ToStatus.Should().Be(OrderStatus.Submitted);
        }
    }
}
