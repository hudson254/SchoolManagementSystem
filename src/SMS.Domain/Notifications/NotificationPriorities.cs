using System;

namespace SMS.Domain.Notifications
{
    /// <summary>
    /// Notification severity. Ordering matters: the notification centre renders
    /// Critical first and the unread badge prioritises the most severe unread item.
    /// </summary>
    public static class NotificationPriorities
    {
        /// <summary>Routine, no action implied (e.g. "new student enrolled in your unit").</summary>
        public const string Informational = "Informational";

        /// <summary>Default. The user should be aware of it.</summary>
        public const string Normal = "Normal";

        /// <summary>The user is expected to act, or a deadline is attached.</summary>
        public const string Important = "Important";

        /// <summary>Requires immediate attention (security, approval blocked, maintenance).</summary>
        public const string Critical = "Critical";

        /// <summary>All values, ordered least to most severe.</summary>
        public static readonly string[] All =
        {
            Informational, Normal, Important, Critical
        };

        /// <summary>
        /// Maps an arbitrary stored value onto a known priority. Unknown or null
        /// values degrade to <see cref="Normal"/> rather than throwing, so a legacy
        /// row can never break the notification centre.
        /// </summary>
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Normal;

            var trimmed = value.Trim();
            foreach (var known in All)
            {
                if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase))
                    return known;
            }

            return Normal;
        }

        /// <summary>True when the value is one of the four known priorities.</summary>
        public static bool IsKnown(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            System.Array.Exists(All, k => string.Equals(k, value.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>Numeric severity, highest last. Used for ordering only.</summary>
        public static int Rank(string? value)
        {
            switch (Normalize(value))
            {
                case Informational: return 0;
                case Normal: return 1;
                case Important: return 2;
                case Critical: return 3;
                default: return 1;
            }
        }
    }
}