using System;

namespace SMS.Domain.Common
{
    /// <summary>
    /// Order number formatting/parsing helpers for the development-default
    /// numbering scheme (open requirement #1). Kept in the domain so the
    /// generator implementation and any validation stay consistent.
    /// </summary>
    public static class OmsOrderNumber
    {
        /// <summary>Formats the canonical order number: ORD-&lt;yyyy&gt;-&lt;6 digits&gt;.</summary>
        public static string Format(int year, long sequence) =>
            $"ORD-{year:D4}-{sequence:D6}";

        /// <summary>Validates that a candidate order number matches the canonical format.</summary>
        public static bool IsValidFormat(string? orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length > 50)
            {
                return false;
            }

                        // ORD-<4-digit year>-<exactly 6 digits> = 15 chars total.
            return orderNumber.Length == 15
                && orderNumber.StartsWith("ORD-", StringComparison.Ordinal)
                && orderNumber[4] >= '0' && orderNumber[4] <= '9'
                && orderNumber[5] >= '0' && orderNumber[5] <= '9'
                && orderNumber[6] >= '0' && orderNumber[6] <= '9'
                && orderNumber[7] >= '0' && orderNumber[7] <= '9'
                && orderNumber[8] == '-'
                && orderNumber[9] >= '0' && orderNumber[9] <= '9'
                && orderNumber[10] >= '0' && orderNumber[10] <= '9'
                && orderNumber[11] >= '0' && orderNumber[11] <= '9'
                && orderNumber[12] >= '0' && orderNumber[12] <= '9'
                && orderNumber[13] >= '0' && orderNumber[13] <= '9'
                && orderNumber[14] >= '0' && orderNumber[14] <= '9';
        }
    }
}
