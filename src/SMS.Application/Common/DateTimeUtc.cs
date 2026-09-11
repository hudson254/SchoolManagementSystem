namespace SMS.Application.Common
{
    /// <summary>
    /// Normalizes DateTime values so they can be safely persisted to
    /// PostgreSQL 'timestamp with time zone' columns.
    /// </summary>
    public static class DateTimeUtc
    {
        /// <summary>
        /// Returns the supplied value converted to a UTC DateTime, or null
        /// when the input is null.
        /// </summary>
        /// <remarks>
        /// The PostgreSQL driver only accepts DateTimes with a UTC kind when
        /// writing to 'timestamp with time zone' columns. Unspecified values
        /// (for example date-only strings such as "2026-09-01" submitted by
        /// web forms) are interpreted as UTC wall-clock values, matching the
        /// application's date convention. Local values are treated the same
        /// way, which is consistent with the date handling used elsewhere in
        /// the application.
        /// </remarks>
        public static DateTime? From(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            return ToUtc(value.Value);
        }

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
    }
}