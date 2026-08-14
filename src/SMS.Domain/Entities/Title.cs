using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Represents a configurable professional/academic title that can be
    /// associated with a user. Titles are stored separately from personal
    /// names so they never interfere with username generation, file naming,
    /// search, sorting, or authentication.
    /// </summary>
    public class Title : BaseEntity, ITenantAwareEntity
    {
        /// <summary>
        /// Short code for the title (e.g., "Dr", "Prof", "Eng").
        /// Used for lookups and storage.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display text for the title (e.g., "Dr.", "Prof.", "Eng.").
        /// This is the normalized form used in display names.
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// Language code for internationalization (e.g., "en", "sw", "fr").
        /// </summary>
        public string Language { get; set; } = "en";

        /// <summary>
        /// Category of the title (e.g., "Academic", "Military", "Religious").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Whether this title is active and available for selection.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Sort order for display in dropdowns.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Normalized code for case-insensitive lookups.
        /// </summary>
        public string NormalizedCode { get; set; } = string.Empty;
    }
}
