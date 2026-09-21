using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.Linq;
using Xunit;

namespace SMS.UnitTests.OMS
{
    /// <summary>
    /// Tests the OMS order-number format and in-memory sequence generation
    /// convention without requiring a PostgreSQL connection.
    /// </summary>
    public class OmsOrderNumberTest
    {
        /// <summary>
        /// OmsOrderNumber uses a YYYY-NNNNNN canonical format for development use.
        /// </summary>
        [Theory]
        [InlineData(2024, 1, "ORD-2024-000001")]
        [InlineData(2026, 42, "ORD-2026-000042")]
        [InlineData(2026, 123456, "ORD-2026-123456")]
        public void FormatProducesCanonicalSequence(int year, int sequence, string expected)
        {
            var gt = new StubGenerator(year);
            var number = gt.Generate(sequence);
            number.Should().Be(expected);
        }

        [Fact]
        public void SameYear_SequencesAreMonotonic()
        {
            var gt = new StubGenerator(2026);
            var first = gt.Generate(1);
            var second = gt.Generate(2);
            first.Should().Be("ORD-2026-000001");
            second.Should().Be("ORD-2026-000002");
        }
    }

    /// <summary>
    /// Stub that mirrors OrderNumberGenerator.GetNextSequenceNumber behavior for
    /// unit tests without I/O. Delegates formatting to the canonical
    /// OmsOrderNumber.Format so tests and production stay consistent.
    /// </summary>
    internal class StubGenerator
    {
        private readonly int _year;

        public StubGenerator(int year) { _year = year; }

        public string Generate(int sequence)
        {
            if (sequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be greater than zero.");
            }

            return OmsOrderNumber.Format(_year, sequence);
        }
    }
}
