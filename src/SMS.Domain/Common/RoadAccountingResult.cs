namespace SMS.Domain.Common
{
    /// <summary>
    /// Result of a Road Accounting operation. Because the business rules are an
    /// open requirement (#18), operations may legitimately return a NOT-CONFIGURED
    /// result. Callers MUST treat <see cref="IsComputed"/>=false as "no accounting
    /// was performed" — never as a zero-value accounting result.
    /// </summary>
    public sealed record RoadAccountingResult(bool IsComputed, decimal? Amount, string? Message)
    {
        /// <summary>Creates the explicit not-configured result (no calculation performed).</summary>
        public static RoadAccountingResult NotConfigured(string? message = null) => new(
            false,
            null,
            message ?? "Road Accounting business rules are not configured (OMS open requirement #18). No allocation or posting was calculated.");

        /// <summary>Creates a computed result (only valid once rules are confirmed).</summary>
        public static RoadAccountingResult Computed(decimal amount) => new(true, amount, null);
    }
}
