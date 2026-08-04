namespace SMS.API.Options
{
    /// <summary>
    /// Configuration options for the <see cref="SMS.API.Middleware.RateLimitingMiddleware"/>.
    /// Bound from the "RateLimiting" section of appsettings.json (or environment
    /// variables). Keeping these configurable allows an operator to tune the
    /// per-IP rate limit and ban duration without a code change.
    /// </summary>
    public class RateLimitingOptions
    {
        /// <summary>
        /// Maximum number of requests allowed per client IP per window.
        /// </summary>
        public int PermitLimit { get; set; } = 100;

        /// <summary>
        /// Length of the rolling window in minutes.
        /// </summary>
        public int WindowMinutes { get; set; } = 1;

        /// <summary>
        /// How long (in minutes) a client is banned after exceeding the limit.
        /// </summary>
        public int BanDurationMinutes { get; set; } = 5;
    }
}
