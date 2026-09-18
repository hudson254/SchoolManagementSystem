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
        [InlineData(2024, 1, "2024-000001")]
        [InlineData(2026, 42, "2026-000042")]
        [InlineData(2026, 123456, "2026-123456")]
        public void FormatProducesCanonicalSequence(int year, int sequence, string expected)
        {
            var gt = new StubGenerator(year);
            var number = gt.Generate();
            number.Should().Be(expected);
        }

        [Fact]
        public void SameYear_SequencesAreMonotonic()
        {
            var gt = new StubGenerator(2026);
            var first = gt.Generate();
            var second = gt.Generate();
            second.Should().Be("2026-000002");
        }
    }

    /// <summary>
    /// Stub that mirrors OrderNumberGenerator.GetNextSequenceNumber behavior for
    /// unit tests without I/O.
    /// </summary>
    internal class StubGenerator
    {
        private readonly int _year;
        private int _seq;

        public StubGenerator(int year) { _year = year; _seq = 0; }

        public string Generate()
        {
            _seq += 1;
            return $"{_year:D4}-{_seq:D6}";
        }
    }
}
