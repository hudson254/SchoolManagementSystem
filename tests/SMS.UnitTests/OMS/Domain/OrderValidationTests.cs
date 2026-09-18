using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Order factory validation tests (Phase 2).
    /// Verifies the development defaults documented in
    /// Documentation/OMS/OMS_OPEN_REQUIREMENTS.md #8 (required header fields).
    /// </summary>
    public class OrderValidationTests
    {
        [Fact]
        public void Create_WithValidInput_HasDraftStatus()
        {
            var order = Order.Create("ORD-2026-000001", "Purchase laptops", null, "user-1", "KES", null, null);
            order.TenantId = Guid.NewGuid();
            order.Status.ToString().Should().Be("Draft");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyOrderNumber_Throws(string? badNumber)
        {
            var ex = Record.Exception(() =>
                Order.Create(badNumber!, "title", null, "user-1", "KES", null, null));
            ex.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        [InlineData("ord-2026-000001")]   // wrong prefix case
        [InlineData("ORD-2026-0001")]     // too short sequence
        [InlineData("ORD-2026-0000001")]  // too long sequence
        [InlineData("ORD-26-000001")]     // wrong year digits
        [InlineData("ORD-2026-ABCDEF")]   // non-digits
        [InlineData("INV-2026-000001")]  // wrong prefix
        public void Create_WithMalformedOrderNumber_Throws(string badNumber)
        {
            var ex = Record.Exception(() =>
                Order.Create(badNumber, "title", null, "user-1", "KES", null, null));
            ex.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("canonical format");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyTitle_Throws(string? badTitle)
        {
            var ex = Record.Exception(() =>
                Order.Create("ORD-2026-000001", badTitle!, null, "user-1", "KES", null, null));
            ex.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("title");
        }

        [Fact]
        public void Create_WithTitleOver200_Throws()
        {
            var longTitle = new string('x', 201);
            var ex = Record.Exception(() =>
                Order.Create("ORD-2026-000001", longTitle, null, "user-1", "KES", null, null));
            ex.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("200");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyRequestedByUserId_Throws(string? badUser)
        {
            var ex = Record.Exception(() =>
                Order.Create("ORD-2026-000001", "title", null, badUser!, "KES", null, null));
            ex.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("Requesting user");
        }

        [Fact]
        public void OrderNumber_IsImmutableAfterCreation()
        {
            var order = Order.Create("ORD-2026-000001", "title", null, "user-1", "KES", null, null);
            order.TenantId = Guid.NewGuid();
            // OrderNumber has only a private setter, so callers cannot change it.
            // GetSetMethod(nonPublic: false) returns null when no PUBLIC setter exists.
            var prop = typeof(Order).GetProperty(nameof(Order.OrderNumber));
            prop.Should().NotBeNull();
            prop!.GetSetMethod(nonPublic: false).Should().BeNull();
        }

        [Fact]
        public void AddItem_AddsItemAndRecalculatesTotal()
        {
            var order = Order.Create("ORD-2026-000001", "title", null, "user-1", "KES", null, null);
            order.TenantId = Guid.NewGuid();

            order.AddItem(null, "Widget", 2, 100m);

            order.TotalAmount.Should().Be(200m);
            order.Items.Should().HaveCount(1);
        }

        [Fact]
        public void RemoveItem_RemovesItemAndRecalculatesTotal()
        {
            var order = Order.Create("ORD-2026-000001", "title", null, "user-1", "KES", null, null);
            order.TenantId = Guid.NewGuid();
            var item = order.AddItem(null, "Widget", 2, 100m);

            order.RemoveItem(item.Id);

            order.Items.Should().BeEmpty();
            order.TotalAmount.Should().Be(0m);
        }

        [Fact]
        public void AddItem_AfterSubmit_Throws()
        {
            var order = Order.Create("ORD-2026-000001", "title", null, "user-1", "KES", null, null);
            order.TenantId = Guid.NewGuid();
            order.AddItem(null, "Widget", 1, 10m); // submit requires >= 1 item (open requirement #8)
            order.Submit("user-1", "User");

            order.Invoking(o => o.AddItem(null, "Widget", 1, 10m))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*Draft*");
        }

        [Fact]
        public void RemoveItem_AfterSubmit_Throws()
        {
            var order = Order.Create("ORD-2026-000001", "title", null, "user-1", "KES", null, null);
            order.TenantId = Guid.NewGuid();
            var item = order.AddItem(null, "Widget", 1, 10m);
            order.Submit("user-1", "User");

            order.Invoking(o => o.RemoveItem(item.Id))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*Draft*");
        }

        [Fact]
        public void Submit_WithoutItems_Throws()
        {
            // Open requirement #8: an order must have at least one item to be submitted.
            var order = Order.Create("ORD-2026-000001", "title", null, "user-1", "KES", null, null);
            order.TenantId = Guid.NewGuid();

            order.Invoking(o => o.Submit("user-1", "User"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*at least one item*");
        }
    }
}
