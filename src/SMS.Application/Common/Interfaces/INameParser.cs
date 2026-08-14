using SMS.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Centralized name parsing service that detects, extracts, and normalizes
    /// professional/academic titles from full name strings. All modules in the
    /// system must use this service for name processing to ensure consistent
    /// title handling.
    /// </summary>
    public interface INameParser
    {
        /// <summary>
        /// Parses a full name string, extracting any recognized title from the
        /// beginning or end of the string. Returns a NameParseResult with
        /// Title, FirstName, MiddleName, LastName, and DisplayName.
        /// </summary>
        /// <param name="fullName">The full name to parse (e.g., "Dr. John Peter Mwangi").</param>
        /// <returns>A NameParseResult with title separated from personal names.</returns>
        Task<NameParseResult> ParseNameAsync(string fullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses a full name string synchronously. Uses the default title
        /// configuration. For database-backed titles, use ParseNameAsync.
        /// </summary>
        NameParseResult ParseName(string fullName);

        /// <summary>
        /// Normalizes a title string to its canonical form (e.g., "DR" → "Dr.",
        /// "dr" → "Dr.", "DR." → "Dr.").
        /// </summary>
        string NormalizeTitle(string title);

        /// <summary>
        /// Checks if a title code is recognized in the current configuration.
        /// </summary>
        bool IsRecognizedTitle(string title);

        /// <summary>
        /// Generates a display name from individual name parts, including
        /// the title if provided.
        /// </summary>
        string GenerateDisplayName(string? title, string firstName, string? middleName, string lastName);

        /// <summary>
        /// Generates a sort key from a full name, ignoring any titles.
        /// Used for alphabetical sorting.
        /// </summary>
        string GenerateSortKey(string fullName);

        /// <summary>
        /// Sanitizes a name for use in file names, removing titles and
        /// invalid characters.
        /// </summary>
        string SanitizeForFileName(string fullName);
    }
}
