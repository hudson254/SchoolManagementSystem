using System;

namespace SMS.Domain.Common
{
    /// <summary>
    /// SMS request number formatting utility.
    /// Format: REQ-<yyyy>-<6-digit sequence>
    /// </summary>
    public static class SmsRequestNumber
    {
        public static string Format(int year, long sequence)
        {
            if (year < 1900 || year > 2100)
                throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 1900 and 2100.");
            if (sequence < 1 || sequence > 999999)
                throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be between 1 and 999999.");

            return $"REQ-{year}-{sequence:D6}";
        }

        public static bool IsValidFormat(string requestNumber)
        {
            if (string.IsNullOrWhiteSpace(requestNumber))
                return false;

            // Format: REQ-YYYY-NNNNNN
            if (!requestNumber.StartsWith("REQ-"))
                return false;

            var parts = requestNumber.Split('-');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[1], out var year) || year < 1900 || year > 2100)
                return false;

            if (!long.TryParse(parts[2], out var sequence) || sequence < 1 || sequence > 999999)
                return false;

            return true;
        }
    }
}
