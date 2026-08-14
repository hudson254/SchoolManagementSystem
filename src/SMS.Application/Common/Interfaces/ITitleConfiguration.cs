using System.Collections.Generic;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Provides access to the configurable title lookup table.
    /// Titles can be loaded from appsettings.json (default/fallback) or
    /// from the database (admin-managed). The configuration is centralized
    /// so all modules use the same set of recognized titles.
    /// </summary>
    public interface ITitleConfiguration
    {
        /// <summary>
        /// Gets all active title entries for the specified language.
        /// Each entry contains the code, display text, category, and sort order.
        /// </summary>
        IEnumerable<TitleEntry> GetTitles(string language = "en");

        /// <summary>
        /// Gets a title entry by its code (case-insensitive lookup).
        /// Returns null if the title is not recognized.
        /// </summary>
        TitleEntry? GetTitleByCode(string code, string language = "en");

        /// <summary>
        /// Checks if a title code is recognized (case-insensitive).
        /// </summary>
        bool IsRecognized(string code, string language = "en");

        /// <summary>
        /// Normalizes a title string to its canonical display form.
        /// e.g., "DR" → "Dr.", "dr" → "Dr.", "DR." → "Dr."
        /// Returns null if the title is not recognized.
        /// </summary>
        string? Normalize(string title, string language = "en");
    }

    /// <summary>
    /// Represents a single title entry in the configuration.
    /// </summary>
    public class TitleEntry
    {
        /// <summary>
        /// Short code for the title (e.g., "Dr", "Prof", "Eng").
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display text for the title (e.g., "Dr.", "Prof.", "Eng.").
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// Language code (e.g., "en", "sw", "fr").
        /// </summary>
        public string Language { get; set; } = "en";

        /// <summary>
        /// Category of the title (e.g., "Academic", "Military", "Religious").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Whether this title is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Sort order for display.
        /// </summary>
        public int SortOrder { get; set; } = 0;
    }
}
