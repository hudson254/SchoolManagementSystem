using Microsoft.Extensions.Options;
using SMS.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Provides access to the configurable title lookup table.
    /// Titles are loaded from appsettings.json (default/fallback) and can be
    /// overridden by database-backed titles managed by administrators.
    /// </summary>
    public class TitleConfiguration : ITitleConfiguration
    {
        private readonly Dictionary<string, List<TitleEntry>> _titlesByLanguage;
        private readonly Dictionary<string, TitleEntry> _titlesByCode;

        public TitleConfiguration(IOptions<TitleOptions> options)
        {
            _titlesByLanguage = new Dictionary<string, List<TitleEntry>>(StringComparer.OrdinalIgnoreCase);
            _titlesByCode = new Dictionary<string, TitleEntry>(StringComparer.OrdinalIgnoreCase);

            if (options?.Value?.Titles != null)
            {
                foreach (var entry in options.Value.Titles)
                {
                    if (!entry.IsActive)
                        continue;

                    var lang = entry.Language ?? "en";
                    if (!_titlesByLanguage.ContainsKey(lang))
                        _titlesByLanguage[lang] = new List<TitleEntry>();

                    _titlesByLanguage[lang].Add(entry);
                    _titlesByCode[entry.Code.ToUpperInvariant()] = entry;
                }
            }
        }

        public IEnumerable<TitleEntry> GetTitles(string language = "en")
        {
            if (_titlesByLanguage.TryGetValue(language, out var entries))
                return entries.OrderBy(e => e.SortOrder).ThenBy(e => e.Code);

            return Enumerable.Empty<TitleEntry>();
        }

        public TitleEntry? GetTitleByCode(string code, string language = "en")
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // Try language-specific lookup first
            if (_titlesByLanguage.TryGetValue(language, out var entries))
            {
                var match = entries.FirstOrDefault(e =>
                    e.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match;
            }

            // Fall back to any language
            if (_titlesByCode.TryGetValue(code.ToUpperInvariant(), out var globalMatch))
                return globalMatch;

            return null;
        }

        public bool IsRecognized(string code, string language = "en")
        {
            return GetTitleByCode(code, language) != null;
        }

        public string? Normalize(string title, string language = "en")
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            // Remove trailing periods and whitespace
            var cleaned = title.Trim().TrimEnd('.').Trim();

            var entry = GetTitleByCode(cleaned, language);
            if (entry != null)
                return entry.DisplayText;

            return null;
        }
    }

    /// <summary>
    /// Options class for configuring titles via appsettings.json.
    /// </summary>
    public class TitleOptions
    {
        public List<TitleEntry> Titles { get; set; } = new List<TitleEntry>();
    }
}
