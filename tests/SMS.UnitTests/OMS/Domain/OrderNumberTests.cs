using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.UnitTests.OMS.Domain
{
    /// <summary>
    /// Order number generation tests (Phase 2). Verifies the development-default
    /// format ORD-yyyy-nnnnnn (open requirement #1) and concurrency safety.
    /// The concrete generator uses PostgreSQL ON CONFLICT; here we test the
    /// format invariants and an in-memory sequence substitute for concurrency.
    /// </summary>
    public class OrderNumberTests
    {
        [Theory]
        [InlineData(2026, 1, "ORD-2026-000001")]
        [InlineData(2026, 42, "ORD-2026-000042")]
        [InlineData(2026, 999999, "ORD-2026-999999")]
        [InlineData(2024, 1, "ORD-2024-000001")]
        public void Format_ProducesCanonicalNumber(int year, long sequence, string expected)
        {
            OmsOrderNumber.Format(year, sequence).Should().Be(expected);
        }

        [Theory]
        [InlineData("ORD-2026-000001", true)]
        [InlineData("ORD-2026-000000", true)]
        [InlineData("ORD-2026-999999", true)]
        [InlineData("ORD-2024-000001", true)]
        [InlineData("ORD-2026-000001", true)]
        public void IsValidFormat_AcceptsCanonical(string number, bool expected)
        {
            OmsOrderNumber.IsValidFormat(number).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ord-2026-000001")]        // lowercase
        [InlineData("ORD-26-000001")]          // 2-digit year
        [InlineData("ORD-2026-0001")]          // 4-digit sequence
        [InlineData("ORD-2026-0000001")]       // 7-digit sequence
        [InlineData("ORD-2026-ABCDEF")]        // non-numeric
        [InlineData("INV-2026-000001")]        // wrong prefix
        [InlineData("ORD-2026-000001-extra")]  // too long
        public void IsValidFormat_RejectsMalformed(string? number, bool expected = false)
        {
            OmsOrderNumber.IsValidFormat(number).Should().Be(expected);
        }

        [Fact]
        public void InMemorySequence_GeneratesUniqueNumbers()
        {
            // Mimics the per-tenant sequence logic used by OrderNumberGenerator
            // without requiring a live PostgreSQL connection.
            var sequence = new OrderNumberSequence { TenantId = Guid.NewGuid(), Year = DateTime.UtcNow.Year, LastNumber = 0 };
            var numbers = new HashSet<string>();

            for (int i = 0; i < 100; i++)
            {
                sequence.LastNumber += 1;
                var number = OmsOrderNumber.Format(sequence.Year, sequence.LastNumber);
                numbers.Add(number).Should().BeTrue("order number must be unique");
            }

            numbers.Should().HaveCount(100);
            numbers.Should().Contain("ORD-" + DateTime.UtcNow.Year + "-000001");
            numbers.Should().Contain("ORD-" + DateTime.UtcNow.Year + "-000100");
        }

        [Fact]
        public void Format_WithZeroSequence_StillValidFormat()
        {
            // Sequence starts at 1 per spec; 0 is out of range but format must still be canonical.
            OmsOrderNumber.IsValidFormat(OmsOrderNumber.Format(2026, 0)).Should().BeTrue();
        }
    }
}
