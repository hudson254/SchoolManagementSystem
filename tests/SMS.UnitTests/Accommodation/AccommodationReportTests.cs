using FluentAssertions;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Application.Features.Accommodation.Queries.Reports;
using SMS.Domain.Enums;
using SMS.Domain.Reporting;
using SMS.Domain.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SMS.UnitTests.Accommodation
{
    /// <summary>
    /// DEFECT-02 — the occupancy-by-period summary never assigned
    /// <c>HousesAtFullCapacity</c> / <c>HousesWithAvailableCapacity</c> (or
    /// <c>HousesNeverOccupied</c>), so the three "capacity pressure" tiles on the
    /// Accommodation Reports page rendered 0 while the house reports showed the
    /// real numbers. The classification now lives in
    /// <see cref="AccommodationCapacityRules"/>, shared by every report summary,
    /// and these tests pin the values down so the tile and the data cannot drift
    /// apart again.
    /// </summary>
    public class AccommodationCapacitySummaryTests
    {
        /// <summary>
        /// Mirrors the anonymous projection the repository builds: capacity, the
        /// occupant count for the report's window, and the all-time record count.
        /// </summary>
        private sealed class HouseFacts
        {
            public int Capacity { get; init; }
            public int OccupiedCount { get; init; }
            public int TotalRecords { get; init; }
        }

        private static List<HouseFacts> Facts(params (int capacity, int occupied, int records)[] houses)
            => houses
                .Select(h => new HouseFacts
                {
                    Capacity = h.capacity,
                    OccupiedCount = h.occupied,
                    TotalRecords = h.records,
                })
                .ToList();

        private static int CountFull(List<HouseFacts> houses)
            => AccommodationCapacityRules.CountAtFullCapacity(houses, h => h.Capacity, h => h.OccupiedCount);

        private static int CountWithFreeSpace(List<HouseFacts> houses)
            => AccommodationCapacityRules.CountWithAvailableCapacity(houses, h => h.Capacity, h => h.OccupiedCount);

        private static int CountNeverOccupied(List<HouseFacts> houses)
            => AccommodationCapacityRules.CountNeverOccupied(houses, h => h.TotalRecords);

        [Fact]
        public void IsAtFullCapacity_WhenCapacityIsPositiveAndEverySpaceIsTaken_ShouldBeTrue()
        {
            AccommodationCapacityRules.IsAtFullCapacity(4, 4).Should().BeTrue();
        }

        [Fact]
        public void IsAtFullCapacity_WhenOverCapacity_ShouldStillBeFull()
        {
            // Capacity can be reduced after allocation, leaving more occupants than
            // spaces. The house is full, not "under pressure".
            AccommodationCapacityRules.IsAtFullCapacity(2, 3).Should().BeTrue();
        }

        [Fact]
        public void IsAtFullCapacity_WhenCapacityIsZero_ShouldNotCountAsFull()
        {
            // Nothing to fill: reporting a zero-capacity house as "full" would inflate
            // the tile with bad data instead of surfacing it.
            AccommodationCapacityRules.IsAtFullCapacity(0, 0).Should().BeFalse();
            AccommodationCapacityRules.IsAtFullCapacity(-1, 0).Should().BeFalse();
        }

        [Fact]
        public void HasAvailableCapacity_WhenUnderCapacity_ShouldBeTrue()
        {
            AccommodationCapacityRules.HasAvailableCapacity(4, 3).Should().BeTrue();
        }

        [Fact]
        public void HasAvailableCapacity_WhenExactlyFull_ShouldBeFalse()
        {
            AccommodationCapacityRules.HasAvailableCapacity(4, 4).Should().BeFalse();
            AccommodationCapacityRules.HasAvailableCapacity(4, 5).Should().BeFalse();
        }

        [Fact]
        public void CountAtFullCapacity_ShouldCountOnlyCompletelyFilledHouses()
        {
            // capacity, occupants in window, all-time records
            var houses = Facts(
                (4, 4, 9),  // full
                (4, 2, 5),  // free space
                (2, 2, 2),  // full
                (0, 0, 0),  // zero capacity -> excluded from "full"
                (6, 0, 3)); // empty

            CountFull(houses).Should().Be(2);
        }

        [Fact]
        public void CountWithAvailableCapacity_ShouldCountEveryHouseNotFilledToCapacity()
        {
            var houses = Facts(
                (4, 4, 9),  // no free space
                (4, 2, 5),  // free space
                (2, 2, 2),  // no free space
                (6, 0, 3)); // empty, all 6 spaces free

            CountWithFreeSpace(houses).Should().Be(2);
        }

        [Fact]
        public void CountNeverOccupied_ShouldUseTheAllTimeRecordCount()
        {
            var houses = Facts(
                (4, 4, 9),  // currently occupied, has history
                (4, 0, 1),  // vacant now but has an assignment record
                (6, 0, 0),  // never occupied
                (2, 0, 0)); // never occupied

            CountNeverOccupied(houses).Should().Be(2);
        }

        [Fact]
        public void NeverOccupied_HouseIsNotCountedWhenItHasARecordOutsideTheWindow()
        {
            // Guards the semantics: "never occupied" is an all-time question, so it is
            // driven by TotalRecords and never contradicts the per-window count.
            var houses = Facts((4, 0, 1));

            CountNeverOccupied(houses).Should().Be(0);
        }

        [Fact]
        public void AllThreeCounts_ShouldBeComputedFromTheSameHouseSet()
        {
            // The bug was one report rendering 0 for these tiles while another report
            // rendered real numbers. Same input -> same numbers, whichever report asks.
            var houses = Facts((4, 4, 9), (4, 2, 5), (2, 2, 2), (0, 0, 0), (6, 0, 3));

            CountFull(houses).Should().Be(2);
            CountWithFreeSpace(houses).Should().Be(2);
            CountNeverOccupied(houses).Should().Be(1);
        }

        [Fact]
        public void Counts_WhenNoHousesMatchTheFilters_ShouldAllBeZero()
        {
            var houses = Facts();

            CountFull(houses).Should().Be(0);
            CountWithFreeSpace(houses).Should().Be(0);
            CountNeverOccupied(houses).Should().Be(0);
        }

        [Fact]
        public void Counts_WhenTheSequenceIsNull_ShouldReturnZeroRatherThanThrow()
        {
            List<HouseFacts>? none = null;

            AccommodationCapacityRules.CountAtFullCapacity(none, h => h.Capacity, h => h.OccupiedCount)
                .Should().Be(0);
            AccommodationCapacityRules.CountWithAvailableCapacity(none, h => h.Capacity, h => h.OccupiedCount)
                .Should().Be(0);
            AccommodationCapacityRules.CountNeverOccupied(none, h => h.TotalRecords)
                .Should().Be(0);
        }

        [Fact]
        public void UniformCapacityEstate_ShouldClassifyOccupiedAndVacantHousesOppositely()
        {
            // Mirrors production, where every active house has Capacity = 1:
            // an occupied house is full and has no free space; a vacant house is the
            // other way round. This is the exact shape the defect was observed in.
            var houses = Facts((1, 1, 2), (1, 0, 1), (1, 0, 0), (1, 0, 0));

            CountFull(houses).Should().Be(1);
            CountWithFreeSpace(houses).Should().Be(3);
            CountNeverOccupied(houses).Should().Be(2);
        }
    }

    /// <summary>
    /// Unit tests for the accommodation report read models and helpers.
    ///
    /// Everything here runs without a database: occupancy date rules (proper
    /// interval overlap, never equality), paging clamps, applied-filter echo,
    /// export file naming and report metadata.
    /// </summary>
    public class AccommodationReportTests
    {
        private static readonly DateTime Jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Mar31 = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        // ===== Occupancy date rules =====

        [Fact]
        public void NormalizePeriod_WhenEndIsDateOnly_ShouldExtendToEndOfDay()
        {
            var (start, end) = OccupancyDateRules.NormalizePeriod(Jan1, Mar31);

            start.Should().Be(Jan1);
            end.Should().Be(Mar31.Date.AddDays(1).AddTicks(-1));
            end.Date.Should().Be(Mar31.Date, "a date-only bound covers the whole day");
        }

        [Fact]
        public void NormalizePeriod_WhenBoundsMissing_ShouldUseMinAndMax()
        {
            var (start, end) = OccupancyDateRules.NormalizePeriod(null, null);

            start.Should().Be(DateTime.MinValue);
            end.Should().Be(DateTime.MaxValue);
        }

        [Fact]
        public void ValidatePeriod_WhenFromIsAfterTo_ShouldReturnError()
        {
            OccupancyDateRules.ValidatePeriod(Mar31, Jan1).Should().NotBeNull();
        }

        [Fact]
        public void ValidatePeriod_WhenFromEqualsTo_ShouldBeValid()
        {
            OccupancyDateRules.ValidatePeriod(Jan1, Jan1).Should().BeNull();
        }

        [Fact]
        public void ValidatePeriod_WhenOnlyOneBoundSupplied_ShouldBeValid()
        {
            OccupancyDateRules.ValidatePeriod(Jan1, null).Should().BeNull();
            OccupancyDateRules.ValidatePeriod(null, Mar31).Should().BeNull();
        }

        [Fact]
        public void OverlapsPeriod_WhenStayStillOpen_ShouldOverlap()
        {
            var periodStart = new DateTime(2026, 2, 1);
            var periodEnd = new DateTime(2026, 2, 28);

            // Occupant moved in before the period and never vacated.
            OccupancyDateRules.OverlapsPeriod(Jan1, null, periodStart, periodEnd).Should().BeTrue();
        }

        [Fact]
        public void OverlapsPeriod_WhenStayVacatedBeforePeriod_ShouldNotOverlap()
        {
            var periodStart = new DateTime(2026, 2, 1);
            var periodEnd = new DateTime(2026, 2, 28);

            OccupancyDateRules.OverlapsPeriod(Jan1, new DateTime(2026, 1, 15), periodStart, periodEnd)
                .Should().BeFalse();
        }

        [Fact]
        public void OverlapsPeriod_WhenStayStartsAfterPeriod_ShouldNotOverlap()
        {
            var periodStart = new DateTime(2026, 2, 1);
            var periodEnd = new DateTime(2026, 2, 28);

            OccupancyDateRules.OverlapsPeriod(new DateTime(2026, 3, 15), null, periodStart, periodEnd)
                .Should().BeFalse();
        }

        [Fact]
        public void OverlapsPeriod_WhenStayTouchesPeriodBoundaries_ShouldOverlap()
        {
            var periodStart = new DateTime(2026, 2, 1);
            var periodEnd = new DateTime(2026, 2, 28);

            // Ends exactly on the first day of the period...
            OccupancyDateRules.OverlapsPeriod(Jan1, periodStart, periodStart, periodEnd).Should().BeTrue();

            // ...and starts exactly on the last day.
            OccupancyDateRules.OverlapsPeriod(periodEnd, null, periodStart, periodEnd).Should().BeTrue();
        }

        [Fact]
        public void DurationInDays_WhenEndBeforeStart_ShouldNeverBeNegative()
        {
            OccupancyDateRules.DurationInDays(Jan1, new DateTime(2025, 12, 1)).Should().Be(0);
        }

        [Fact]
        public void DurationInDays_WhenStillOccupying_ShouldUseReferenceDate()
        {
            OccupancyDateRules.DurationInDays(Jan1, null, new DateTime(2026, 1, 31)).Should().Be(30);
        }

        // ===== Interval semantics must be unchanged by the date fix =====

        [Fact]
        public void OverlapsPeriod_WithUtcBounds_ShouldKeepBoundaryEqualityInclusive()
        {
            var periodStart = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

            // Boundary equality stays INCLUSIVE on both ends.
            OccupancyDateRules.OverlapsPeriod(periodEnd, null, periodStart, periodEnd).Should().BeTrue();
            OccupancyDateRules.OverlapsPeriod(periodStart, periodStart, periodStart, periodEnd).Should().BeTrue();
        }

        [Fact]
        public void OverlapsPeriod_WithOpenEndedStayAcrossUtcPeriod_ShouldOverlap()
        {
            var (periodStart, periodEnd) = OccupancyDateRules.NormalizePeriod(
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Unspecified));

            // Moved in before the period and never vacated (VacatedDate IS NULL).
            OccupancyDateRules.OverlapsPeriod(Jan1, null, periodStart, periodEnd).Should().BeTrue();
        }

        [Fact]
        public void OverlapsPeriod_WithSameDayRange_ShouldStillCoverTheWholeDay()
        {
            // fromDate == toDate: NormalizePeriod must still cover the WHOLE day.
            var (periodStart, periodEnd) = OccupancyDateRules.NormalizePeriod(
                new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Unspecified));

            periodStart.Date.Should().Be(new DateTime(2026, 3, 15));
            periodEnd.Date.Should().Be(new DateTime(2026, 3, 15));
            periodEnd.TimeOfDay.Should().BeGreaterThan(TimeSpan.FromHours(23));

            var midday = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
            OccupancyDateRules.OverlapsPeriod(midday, null, periodStart, periodEnd).Should().BeTrue();

            var nextDay = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc);
            OccupancyDateRules.OverlapsPeriod(nextDay, null, periodStart, periodEnd).Should().BeFalse();
        }

        [Fact]
        public void OverlapsPeriod_WithStayEndingBeforeUtcPeriod_ShouldNotOverlap()
        {
            var (periodStart, periodEnd) = OccupancyDateRules.NormalizePeriod(
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Unspecified));

            OccupancyDateRules.OverlapsPeriod(Jan1, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), periodStart, periodEnd)
                .Should().BeFalse();
        }

        // ===== Query filters / paging =====

        [Fact]
        public void ToFilters_WhenPagingIsInvalid_ShouldApplyDefaults()
        {
            var query = new GetCurrentOccupancyReportQuery { Page = 0, PageSize = -10 };

            var filters = query.ToFilters();

            filters.Page.Should().Be(1);
            filters.PageSize.Should().Be(50);
        }

        [Fact]
        public void ToFilters_WhenMaxPageSizeSupplied_ShouldClamp()
        {
            var query = new GetOccupancyHistoryReportQuery { PageSize = 100000 };

            query.ToFilters(200).PageSize.Should().Be(200);
        }

        [Fact]
        public void ToFilters_ShouldCopyEveryFilter()
        {
            var laneId = Guid.NewGuid();
            var houseId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();
            var academicYearId = Guid.NewGuid();
            var query = new GetOccupancyHistoryReportQuery
            {
                LaneId = laneId,
                HouseId = houseId,
                Status = "Occupied",
                OccupantType = OccupantType.Lecturer,
                SemesterId = semesterId,
                AcademicYearId = academicYearId,
                FromDate = Jan1,
                ToDate = Mar31,
                SearchTerm = "JOHN",
                Page = 3,
                PageSize = 25
            };

            var filters = query.ToFilters();

            filters.LaneId.Should().Be(laneId);
            filters.HouseId.Should().Be(houseId);
            filters.Status.Should().Be("Occupied");
            filters.OccupantType.Should().Be(OccupantType.Lecturer);
            filters.SemesterId.Should().Be(semesterId);
            filters.AcademicYearId.Should().Be(academicYearId);
            filters.FromDate.Should().Be(Jan1);
            filters.ToDate.Should().Be(Mar31);
            filters.SearchTerm.Should().Be("JOHN");
            filters.Page.Should().Be(3);
            filters.PageSize.Should().Be(25);
        }

        // ===== Date/time contract (regression: HTTP 500 on unspecified kinds) =====

        [Theory]
        [InlineData("2026-10-01T00:00:00")]      // no designator -> Unspecified
        [InlineData("2026-10-01T00:00:00Z")]     // explicit UTC
        [InlineData("2026-10-01T00:00:00+03:00")]
        [InlineData("2026-10-01")]
        [InlineData("2026-10-01T12:34:56")]
        public void NormalizeBound_AnySuppliedRepresentation_ShouldYieldUtc(string raw)
        {
            // Parsed the way ASP.NET Core binds a query-string value.
            var bound = DateTime.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);

            var result = OccupancyDateRules.NormalizeBound(bound);

            result.Should().NotBeNull();
            result!.Value.Kind.Should().Be(DateTimeKind.Utc,
                "a timestamp with time zone column only accepts UTC");
        }

        [Fact]
        public void NormalizeBound_WhenMissing_ShouldStayNull()
        {
            OccupancyDateRules.NormalizeBound(null).Should().BeNull();
        }

        [Fact]
        public void NormalizeBound_WhenUtc_ShouldKeepExactInstant()
        {
            var utc = new DateTime(2026, 10, 1, 3, 4, 5, DateTimeKind.Utc);

            OccupancyDateRules.NormalizeBound(utc).Should().Be(utc);
        }

        [Fact]
        public void NormalizeBound_WhenUnspecified_ShouldKeepWallClockValue()
        {
            // The reported production failure: "2026-10-01T00:00:00" binds Unspecified.
            var unspecified = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified);

            var result = OccupancyDateRules.NormalizeBound(unspecified);

            result.Should().NotBeNull();
            result!.Value.Kind.Should().Be(DateTimeKind.Utc);
            result.Value.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                "an offset-less value is read as the UTC wall clock the caller wrote");
        }

        [Fact]
        public void NormalizePeriod_WhenBoundsMissing_ShouldStillReturnUtcKind()
        {
            // DateTime.MinValue / MaxValue are Unspecified by default and would
            // reproduce the same Npgsql failure on the open-ended reports.
            var (start, end) = OccupancyDateRules.NormalizePeriod(null, null);

            start.Kind.Should().Be(DateTimeKind.Utc);
            end.Kind.Should().Be(DateTimeKind.Utc);
            start.Should().Be(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            end.Should().Be(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
        }

        [Fact]
        public void NormalizePeriod_WhenBoundsUnspecified_ShouldReturnUtcKind()
        {
            var (start, end) = OccupancyDateRules.NormalizePeriod(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Unspecified));

            start.Kind.Should().Be(DateTimeKind.Utc);
            end.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ValidatePeriod_WhenFromIsAfterTo_ShouldThrowValidationException()
        {
            var query = new GetOccupancyByPeriodReportQuery { FromDate = Mar31, ToDate = Jan1 };

            var act = () => query.ValidatePeriod();

            act.Should().Throw<ValidationException>().WithMessage("*From date*");
        }

        [Theory]
        [InlineData("2026-10-01T00:00:00")]
        [InlineData("2026-10-01T00:00:00Z")]
        [InlineData("2026-10-01T00:00:00+03:00")]
        public void ToFilters_ShouldNormalizeEveryDateRepresentationToUtc(string raw)
        {
            var query = new GetOccupancyHistoryReportQuery
            {
                FromDate = DateTime.Parse(raw, System.Globalization.CultureInfo.InvariantCulture),
                ToDate = DateTime.Parse(raw, System.Globalization.CultureInfo.InvariantCulture)
            };

            var filters = query.ToFilters();

            filters.FromDate.Should().NotBeNull();
            filters.FromDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
            filters.ToDate.Should().NotBeNull();
            filters.ToDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void ToFilters_WhenDatesAreUtc_ShouldNotShiftThem()
        {
            var query = new GetOccupancyHistoryReportQuery { FromDate = Jan1, ToDate = Mar31 };

            var filters = query.ToFilters();

            filters.FromDate.Should().Be(Jan1);
            filters.ToDate.Should().Be(Mar31);
        }

        [Fact]
        public void ToFilters_WhenDatesMissing_ShouldLeaveThemNull()
        {
            var filters = new GetOccupancyHistoryReportQuery().ToFilters();

            filters.FromDate.Should().BeNull();
            filters.ToDate.Should().BeNull();
        }

        [Fact]
        public void ToFilters_WhenDatesUnspecified_ShouldNotChangeTheCalendarDay()
        {
            // Normalisation must re-tag the kind only; it must never shift the day
            // the user asked for.
            var query = new GetOccupancyHistoryReportQuery
            {
                FromDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified),
                ToDate = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Unspecified)
            };

            var filters = query.ToFilters();

            filters.FromDate!.Value.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
            filters.ToDate!.Value.Should().Be(new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc));
        }

        [Theory]
        [InlineData("2026-10-01T00:00:00", "2026-09-30T00:00:00")]   // plain -> plain
        [InlineData("2026-10-01T00:00:00Z", "2026-09-30T00:00:00Z")] // Z -> Z
        [InlineData("2026-10-01T00:00:00+03:00", "2026-09-30T00:00:00")]
        public void ValidatePeriod_WithMixedRepresentations_ShouldStillRejectReversedRange(string from, string to)
        {
            var query = new GetOccupancyHistoryReportQuery
            {
                FromDate = DateTime.Parse(from, System.Globalization.CultureInfo.InvariantCulture),
                ToDate = DateTime.Parse(to, System.Globalization.CultureInfo.InvariantCulture)
            };

            var act = () => query.ValidatePeriod();

            act.Should().Throw<ValidationException>("a reversed range must still be a 400, not a 500");
        }

        [Theory]
        [InlineData("2026-10-01T00:00:00", "2026-10-01T00:00:00Z")]  // same instant, mixed kinds
        [InlineData("2026-10-01T00:00:00", "2026-10-31T00:00:00")]
        public void ValidatePeriod_WithForwardOrEqualRangeInAnyRepresentation_ShouldBeValid(string from, string to)
        {
            var query = new GetOccupancyHistoryReportQuery
            {
                FromDate = DateTime.Parse(from, System.Globalization.CultureInfo.InvariantCulture),
                ToDate = DateTime.Parse(to, System.Globalization.CultureInfo.InvariantCulture)
            };

            var act = () => query.ValidatePeriod();

            act.Should().NotThrow("an equal or forward range is valid in every representation");
        }

        [Fact]
        public void ValidatePeriod_WhenEqualRegardlessOfRepresentation_ShouldBeValid()
        {
            var query = new GetOccupancyHistoryReportQuery
            {
                FromDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified),
                ToDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var act = () => query.ValidatePeriod();

            act.Should().NotThrow("fromDate == toDate is a valid same-day range");
        }

        // ===== Report support helpers =====

        [Fact]
        public void BuildPagination_ShouldComputeTotalPages()
        {
            var filters = new AccommodationReportFilters { Page = 3, PageSize = 50 };

            var pagination = AccommodationReportSupport.BuildPagination(filters, 240);

            pagination.TotalCount.Should().Be(240);
            pagination.Page.Should().Be(3);
            pagination.PageSize.Should().Be(50);
            pagination.TotalPages.Should().Be(5);
        }

        [Fact]
        public void BuildPagination_WhenNoRows_ShouldReturnZeroPages()
        {
            var pagination = AccommodationReportSupport.BuildPagination(
                new AccommodationReportFilters { Page = 1, PageSize = 50 }, 0);

            pagination.TotalPages.Should().Be(0);
        }

        [Fact]
        public void BuildAppliedFilters_WhenNoFiltersSupplied_ShouldReportNone()
        {
            var applied = AccommodationReportSupport.BuildAppliedFilters(new AccommodationReportFilters());

            applied.Should().ContainSingle();
            applied[0].Label.Should().Be("Filters");
            applied[0].Value.Should().Be("None");
        }

        [Fact]
        public void BuildAppliedFilters_ShouldEchoHumanReadableValues()
        {
            var laneId = Guid.NewGuid();
            var semesterId = Guid.NewGuid();
            var filters = new AccommodationReportFilters
            {
                LaneId = laneId,
                SemesterId = semesterId,
                OccupantType = OccupantType.Student,
                FromDate = Jan1,
                ToDate = Mar31,
                SearchTerm = "A1"
            };
            var labels = new System.Collections.Generic.Dictionary<string, string>
            {
                ["LaneId"] = "Lane A",
                ["SemesterId"] = "Semester 1 2026"
            };

            var applied = AccommodationReportSupport.BuildAppliedFilters(filters, labels);

            applied.Should().Contain(f => f.Label == "Lane" && f.Value == "Lane A");
            applied.Should().Contain(f => f.Label == "Semester" && f.Value == "Semester 1 2026");
            applied.Should().Contain(f => f.Label == "Occupant type" && f.Value == "Student");
            applied.Should().Contain(f => f.Label == "From" && f.Value == "2026-01-01");
            applied.Should().Contain(f => f.Label == "To" && f.Value == "2026-03-31");
            applied.Should().Contain(f => f.Label == "Search" && f.Value == "A1");
        }

        [Fact]
        public void ApplyMeta_ShouldStampReportIdentityAndFilters()
        {
            var filters = new AccommodationReportFilters { Status = "Vacant" };
            var dto = new AccommodationHouseOccupancyReportDto();

            AccommodationReportSupport.ApplyMeta(
                dto, "empty-houses", "Empty Houses", filters, "coordinator", Jan1);

            dto.ReportKey.Should().Be("empty-houses");
            dto.ReportTitle.Should().Be("Empty Houses");
            dto.GeneratedBy.Should().Be("coordinator");
            dto.GeneratedAtUtc.Should().Be(Jan1);
            dto.AppliedFilters.Should().Contain(f => f.Label == "House status" && f.Value == "Vacant");
        }

        [Fact]
        public void BuildFileName_WithoutPeriod_ShouldStampToday()
        {
            var fileName = AccommodationReportSupport.BuildFileName("Accommodation_Current_Occupancy", null, null, "pdf");

            fileName.Should().StartWith("Accommodation_Current_Occupancy_");
            fileName.Should().EndWith(".pdf");
        }

        [Fact]
        public void BuildFileName_WithPeriod_ShouldIncludeBothBounds()
        {
            var fileName = AccommodationReportSupport.BuildFileName(
                "Accommodation_Occupancy_History", Jan1, Mar31, "xlsx");

            fileName.Should().Be("Accommodation_Occupancy_History_2026-01-01_to_2026-03-31.xlsx");
        }

        [Theory]
        [InlineData("coordinator", "coordinator")]
        [InlineData(null, "coordinator@example.com")]
        [InlineData("", "coordinator@example.com")]
        public void ResolveGeneratedBy_ShouldFallBackToEmail(string? username, string expected)
        {
            var user = new FakeCurrentUser { Username = username!, Email = "coordinator@example.com" };

            AccommodationReportSupport.ResolveGeneratedBy(user).Should().Be(expected);
        }

        [Fact]
        public void ResolveGeneratedBy_WhenUsernameAndEmailAreBlank_ShouldFallBackToUserId()
        {
            var user = new FakeCurrentUser { Username = "", Email = "", UserId = "user-42" };

            AccommodationReportSupport.ResolveGeneratedBy(user).Should().Be("user-42");
        }

        /// <summary>Minimal current-user stub (Domain contract) for the helpers above.</summary>
        private sealed class FakeCurrentUser : Domain.Interfaces.ICurrentUserService
        {
            public string UserId { get; set; } = "user-1";
            public string Username { get; set; } = "coordinator";
            public string Email { get; set; } = "coordinator@example.com";
            public bool IsAuthenticated => true;
            public System.Collections.Generic.IEnumerable<string> Roles => new[] { "Coordinator" };
        }
    }
}
