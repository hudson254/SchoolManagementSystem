using System;
using FluentAssertions;
using SMS.Application.Common;
using Xunit;

namespace SMS.UnitTests.Common
{
    public class DateTimeUtcTests
    {
        [Fact]
        public void From_NullValue_ShouldReturnNull()
        {
            DateTimeUtc.From((DateTime?)null).Should().BeNull();
        }

        [Fact]
        public void From_UtcValue_ShouldReturnSameInstant()
        {
            var utc = new DateTime(2026, 9, 1, 6, 30, 0, DateTimeKind.Utc);

            var result = DateTimeUtc.From(utc);

            result.Should().NotBeNull();
            result.Value.Kind.Should().Be(DateTimeKind.Utc);
            result.Value.Hour.Should().Be(6);
        }

        [Fact]
        public void From_UnspecifiedValue_ShouldInterpretAsUtc()
        {
            // Represents the "2026-09-01" date-only payload sent by web forms.
            var unspecified = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Unspecified);

            var result = DateTimeUtc.From(unspecified);

            result.Should().NotBeNull();
            result.Value.Kind.Should().Be(DateTimeKind.Utc);
            result.Value.Year.Should().Be(2026);
            result.Value.Month.Should().Be(9);
            result.Value.Day.Should().Be(1);
            result.Value.Hour.Should().Be(0);
            result.Value.Minute.Should().Be(0);
        }

        [Fact]
        public void From_LocalValue_ShouldInterpretAsUtc()
        {
            var local = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Local);

            var result = DateTimeUtc.From(local);

            result.Should().NotBeNull();
            result.Value.Kind.Should().Be(DateTimeKind.Utc);
            result.Value.Hour.Should().Be(8);
        }
    }
}