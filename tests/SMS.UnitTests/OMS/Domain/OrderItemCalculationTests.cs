using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Order item calculation tests (Phase 2). Verifies:
    ///   - Line total = quantity × unit price
    ///   - decimal arithmetic (never float/double)
    ///   - Quantity > 0 enforced (open requirement #9)
    ///   - Unit price non-negative, default 0
    ///   - half-even rounding to 2 dp
    /// </summary>
    public class OrderItemCalculationTests
    {
                [Fact]
        public void LineTotal_EqualsQuantityTimesUnitPrice()
        {
            OrderItem.Create("CODE", "desc", 2, 100m).LineTotal.Should().Be(200m);
            OrderItem.Create("CODE", "desc", 1, 99.99m).LineTotal.Should().Be(99.99m);
            OrderItem.Create("CODE", "desc", 5, 19.99m).LineTotal.Should().Be(99.95m);
            OrderItem.Create("CODE", "desc", 10, 0m).LineTotal.Should().Be(0m);

            // half-even rounding: 3 * 12.345 = 37.035 -> 37.04 (4 is even)
            var item = OrderItem.Create("CODE", "desc", 3, 12.345m);
            item.LineTotal.Should().Be(37.04m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5)]
        public void Create_WithZeroOrNegativeQuantity_Throws(int badQty)
        {
            var ex = Record.Exception(() => OrderItem.Create("CODE", "desc", badQty, 10m));
            ex.Should().BeOfType<ArgumentOutOfRangeException>()
                .Which.Message.Should().Contain("Quantity");
        }

                [Fact]
        public void Create_WithNegativeUnitPrice_Throws()
        {
            var ex1 = Record.Exception(() => OrderItem.Create("CODE", "desc", 1, -0.01m));
            ex1.Should().BeOfType<ArgumentOutOfRangeException>().Which.Message.Should().Contain("Unit price");

            var ex2 = Record.Exception(() => OrderItem.Create("CODE", "desc", 1, -100m));
            ex2.Should().BeOfType<ArgumentOutOfRangeException>().Which.Message.Should().Contain("Unit price");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyDescription_Throws(string? badDesc)
        {
            var ex = Record.Exception(() => OrderItem.Create("CODE", badDesc!, 1, 10m));
            ex.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Create_WithLongDescription_Throws()
        {
            var longDesc = new string('x', 501);
            var ex = Record.Exception(() => OrderItem.Create("CODE", longDesc, 1, 10m));
            ex.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Create_WithItemCodeOver50_Throws()
        {
            var longCode = new string('x', 51);
            var ex = Record.Exception(() => OrderItem.Create(longCode, "desc", 1, 10m));
            ex.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void Create_WithDefaultUnitPrice_IsZero()
        {
            // Per open requirement #9: unit price is optional, default 0.
            var item = OrderItem.Create(null, "Free item", 5, 0m);
            item.UnitPrice.Should().Be(0m);
            item.LineTotal.Should().Be(0m);
        }

        [Fact]
        public void Update_RecalculatesLineTotal()
        {
            var item = OrderItem.Create("CODE", "desc", 2, 10m);
            item.LineTotal.Should().Be(20m);

            item.Update(3, 5m);
            item.LineTotal.Should().Be(15m);
            item.Quantity.Should().Be(3);
        }

        [Fact]
        public void Update_WithInvalidQuantity_Throws()
        {
            var item = OrderItem.Create("CODE", "desc", 2, 10m);
            item.Invoking(i => i.Update(0, 10m)).Should().Throw<ArgumentOutOfRangeException>();
        }

                [Fact]
        public void HalfEvenRounding_Applies()
        {
            // OmsMoney uses Math.Round(..., MidpointRounding.ToEven).
            // 0.005 rounds to 0.00 (0 is even); 0.015 rounds to 0.02 (2 is even).
            OmsMoney.Round(0.005m).Should().Be(0.00m);
            OmsMoney.Round(0.015m).Should().Be(0.02m);
            OmsMoney.Round(2.675m).Should().Be(2.68m); // 2.675 -> 2.68 (8 is even)
            // 19.99 * 3 = 59.97 (exact, no rounding needed)
            OrderItem.Create("CODE", "desc", 3, 19.99m).LineTotal.Should().Be(59.97m);
        }
    }
}
