using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Domain.Common
{
    /// <summary>
    /// Result of parsing a full name string into its constituent parts.
    /// Titles are extracted and stored separately so they never leak into
    /// usernames, file names, search tokens, or sort keys.
    /// </summary>
    public class NameParseResult
    {
        /// <summary>
        /// The normalized title code (e.g., "Dr", "Prof") or null/empty if none.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The normalized display text for the title (e.g., "Dr.", "Prof.").
        /// </summary>
        public string? TitleDisplayText { get; set; }

        /// <summary>
        /// The person's first name (title stripped).
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The person's middle name(s) (title stripped), or null if none.
        /// </summary>
        public string? MiddleName { get; set; }

        /// <summary>
        /// The person's last name (title stripped).
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The full display name including title (e.g., "Dr. John Peter Mwangi").
        /// </summary>
        public string DisplayName => BuildDisplayName();

        /// <summary>
        /// The full name without title (e.g., "John Peter Mwangi").
        /// </summary>
        public string FullNameWithoutTitle => string.Join(" ",
            new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        /// <summary>
        /// Whether a title was detected and extracted.
        /// </summary>
        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

        /// <summary>
        /// Any validation warnings encountered during parsing (e.g., unknown title,
        /// duplicate titles, invalid punctuation).
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Whether the name passed validation (no blocking errors).
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        private string BuildDisplayName()
        {
            var parts = new List<string>();
            if (HasTitle && !string.IsNullOrWhiteSpace(TitleDisplayText))
                parts.Add(TitleDisplayText);
            parts.Add(FirstName);
            if (!string.IsNullOrWhiteSpace(MiddleName))
                parts.Add(MiddleName);
            parts.Add(LastName);
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }
    }
}
