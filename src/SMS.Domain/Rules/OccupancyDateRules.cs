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
    ///
    /// <para>
    /// <b>Timezone contract.</b> Every bound that reaches this class is normalised
    /// to <see cref="DateTimeKind.Utc"/> by <see cref="NormalizeBound"/> before
    /// use. PostgreSQL columns are <c>timestamp with time zone</c> and the Npgsql
    /// provider rejects a <see cref="DateTimeKind.Unspecified"/> parameter, which
    /// is exactly what ASP.NET Core model binding produces for a query string such
    /// as <c>fromDate=2026-10-01T00:00:00</c> (no designator). Normalising here —
    /// the layer both the application handlers and the persistence repositories
    /// can reach — keeps the fix in one place instead of at every call site.
    /// </para>
    /// </summary>
    public static class OccupancyDateRules
    {
        /// <summary>
        /// Normalises a report period bound to application-UTC.
        ///
        /// A <see cref="DateTimeKind.Utc"/> value is returned unchanged, so an
        /// explicit <c>Z</c> or numeric offset keeps its exact instant. A bound
        /// without a designator binds as <see cref="DateTimeKind.Unspecified"/>
        /// (and, on some hosts, <see cref="DateTimeKind.Local"/>); it is read as
        /// the UTC wall-clock time the caller wrote. This matches the existing
        /// application-wide convention in <c>SMS.Application.Common.DateTimeUtc</c>
        /// and the way the Accommodations UI builds its parameters.
        ///
        /// Null passes through so callers can keep using "no bound supplied".
        /// </summary>
        public static DateTime? NormalizeBound(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            return ToUtc(value.Value);
        }

        /// <summary>
        /// Reinterprets an unspecified or local wall-clock value as UTC, and
        /// leaves a genuine UTC instant untouched.
        /// </summary>
        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            return new DateTime(
                value.Year,
                value.Month,
                value.Day,
                value.Hour,
                value.Minute,
                value.Second,
                DateTimeKind.Utc);
        }

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
        /// <remarks>
        /// Both bounds are normalised to <see cref="DateTimeKind.Utc"/> so the
        /// values are always safe to send to a <c>timestamp with time zone</c>
        /// column. The sentinel defaults are UTC-tagged for the same reason:
        /// <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>
        /// are <see cref="DateTimeKind.Unspecified"/> by default.
        /// </remarks>
        public static (DateTime Start, DateTime End) NormalizePeriod(DateTime? from, DateTime? to)
        {
            var start = NormalizeBound(from) ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var end = NormalizeBound(to) ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

            // Date-only end bound (no time component) => inclusive end of day.
            if (end.TimeOfDay == TimeSpan.Zero && end != DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))
                end = DateTime.SpecifyKind(end.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

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
