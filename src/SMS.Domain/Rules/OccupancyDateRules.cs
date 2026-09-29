using System;

namespace SMS.Domain.Rules
{
    /// <summary>
    /// Pure date rules for accommodation occupancy reporting.
    ///
    /// All occupancy timestamps are stored in UTC (DateTime.UtcNow). Report
    /// periods arrive as date bounds; a bound with no time component is treated
    /// as a whole inclusive day (00:00:00 for the start, 23:59:59.999... for the
    /// end) so that "31/03/2026" includes occupants allocated during that day.
    /// </summary>
    public static class OccupancyDateRules
    {
        /// <summary>
        /// True when an occupancy interval overlaps the reporting period:
        ///   OccupancyStartDate &lt;= ReportEndDate
        ///   AND (OccupancyEndDate IS NULL OR OccupancyEndDate &gt;= ReportStartDate).
        /// Never uses simple equality checks.
        /// </summary>
        public static bool OverlapsPeriod(DateTime occupancyStart, DateTime? occupancyEnd, DateTime periodStart, DateTime periodEnd)
        {
            return occupancyStart <= periodEnd
                && (occupancyEnd == null || occupancyEnd.Value >= periodStart);
        }

        /// <summary>
        /// True when a date lies inside the inclusive reporting period.
        /// </summary>
        public static bool WithinPeriod(DateTime date, DateTime periodStart, DateTime periodEnd)
        {
            return date >= periodStart && date <= periodEnd;
        }

        /// <summary>
        /// Normalizes the raw report bounds to an inclusive UTC period.
        /// A date-only end bound is extended to the end of that day.
        /// Missing bounds default to the minimum / maximum representable date.
        /// </summary>
        public static (DateTime Start, DateTime End) NormalizePeriod(DateTime? from, DateTime? to)
        {
            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.MaxValue;

            // Date-only end bound (no time component) => inclusive end of day.
            if (end.TimeOfDay == TimeSpan.Zero && end != DateTime.MaxValue)
                end = end.Date.AddDays(1).AddTicks(-1);

            return (start, end);
        }

        /// <summary>
        /// Validates that the period bounds are usable.
        /// Returns an error message, or null when valid.
        /// </summary>
        public static string? ValidatePeriod(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue)
            {
                var (start, end) = NormalizePeriod(from, to);
                if (start > end)
                    return "The From date must be on or before the To date.";
            }
            return null;
        }

        /// <summary>
        /// Whole days between the occupancy start and end (or now for current stays).
        /// Never negative.
        /// </summary>
        public static int DurationInDays(DateTime start, DateTime? end, DateTime? referenceUtc = null)
        {
            var effectiveEnd = end ?? referenceUtc ?? DateTime.UtcNow;
            if (effectiveEnd < start) return 0;
            return (int)(effectiveEnd - start).TotalDays;
        }
    }
}
