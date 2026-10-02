using System;

namespace SMS.Domain.Notifications
{
    /// <summary>
    /// Validation helpers for the values a notification carries. Kept in the Domain
    /// layer so that BOTH the MediatR handlers and any other writer (background
    /// services, the SignalR fan-out) are forced through the same rules.
    /// </summary>
    public static class NotificationCatalog
    {
        /// <summary>Hard cap on a notification title, matching the column width.</summary>
        public const int MaxTitleLength = 200;

        /// <summary>Hard cap on a notification message.</summary>
        public const int MaxMessageLength = 1000;

        /// <summary>Maximum length of the relative action path.</summary>
        public const int MaxActionUrlLength = 512;

        /// <summary>
        /// Normalises an action target to a safe APPLICATION-RELATIVE path, or null.
        /// <para>
        /// Rejects absolute URLs (<c>https://evil.example</c>), protocol-relative URLs
        /// (<c>//evil.example</c>), anything carrying a scheme (<c>javascript:</c>,
        /// <c>data:</c>), and anything that escapes the SPA root. This is what stops a
        /// notification from being used as an open-redirect / XSS vector. Even when a
        /// value survives this check it remains a navigation HINT: the SPA router and
        /// the API behind the target page remain the authorization boundary.
        /// </para>
        /// </summary>
        public static string? NormalizeActionUrl(string? actionUrl)
        {
            if (string.IsNullOrWhiteSpace(actionUrl)) return null;

            var value = actionUrl.Trim();
            if (value.Length == 0 || value.Length > MaxActionUrlLength) return null;

            // Protocol-relative ("//host") and absolute ("https://host", "http://host").
            if (value.StartsWith("//", StringComparison.Ordinal)) return null;

            // Must be root-relative: a single leading slash.
            if (value[0] != '/') return null;

            // Reject any scheme-looking prefix. A path cannot legitimately contain a
            // colon before the first slash, which is exactly where "javascript:alert(1)"
            // and "data:text/html,..." would appear.
            var firstSlash = value.IndexOf('/', 1);
            var segment = firstSlash < 0 ? value.Substring(1) : value.Substring(1, firstSlash - 1);
            if (segment.Contains(':')) return null;

            // Reject control characters and characters browsers normalise away or treat
            // specially: "\" becomes "/" (so "/\evil.example" cannot slip past the
            // protocol-relative check), "#" and "?" start a fragment/query.
            foreach (var c in value)
            {
                if (char.IsControl(c) || c == '\\' || c == '#' || c == '?') return null;
            }

            // Reject traversal segments and collapse duplicate slashes.
            var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (p == "." || p == "..") return null;
            }

            return "/" + string.Join("/", parts);
        }

        /// <summary>Clamps a title to the persisted length, never returning null.</summary>
        public static string NormalizeTitle(string? title)
        {
            var value = (title ?? string.Empty).Trim();
            return value.Length <= MaxTitleLength ? value : value.Substring(0, MaxTitleLength);
        }

        /// <summary>Clamps a message to the persisted length, never returning null.</summary>
        public static string NormalizeMessage(string? message)
        {
            var value = (message ?? string.Empty).Trim();
            return value.Length <= MaxMessageLength ? value : value.Substring(0, MaxMessageLength);
        }
    }
}