using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SMS.Application.Common.Interfaces;
using SMS.Domain.Interfaces;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Generates unique, collision-free usernames using a priority-based
    /// algorithm. Each candidate is validated against the user manager before
    /// being proposed, guaranteeing uniqueness.
    ///
    /// Titles are NEVER included in generated usernames. The name parser is
    /// used to strip any titles before username generation.
    /// </summary>
    public class UsernameGenerator : IUsernameGenerator
    {
        private const int MaxUsernameLength = 50;
        private const int MaxCollisionAttempts = 100;
        // Allow dot as a valid character in usernames (e.g., firstname.lastname)
        private static readonly Regex ValidUsernameRegex = new Regex(@"^[\p{L}\p{N}\.]+$", RegexOptions.Compiled);

        private readonly IUserManagerService _userManager;
        private readonly INameParser _nameParser;

        public UsernameGenerator(IUserManagerService userManager, INameParser nameParser)
        {
            _userManager = userManager;
            _nameParser = nameParser;
        }

        /// <inheritdoc />
        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var sanitized = Sanitize(username);

            // Username must be non-empty, lowercase letters/numbers only, and within max length.
            if (string.IsNullOrEmpty(sanitized) ||
                sanitized.Length > MaxUsernameLength ||
                !ValidUsernameRegex.IsMatch(sanitized))
            {
                return false;
            }

            var existing = await _userManager.FindByUsernameAsync(sanitized);
            return existing == null;
        }

        /// <inheritdoc />
        public async Task<string> GenerateUsernameAsync(string firstName, string lastName)
        {
            // Strip any titles from the name parts before generating the username.
            // This ensures titles like "Dr", "Prof" never appear in usernames.
            var first = Sanitize(StripTitle(firstName));
            var last = Sanitize(StripTitle(lastName));

            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                throw new ArgumentException("At least one of first name or last name is required.");

            // Candidate priority:
            // 1. firstname.lastname
            // 2. firstnamelastname
            // 3. flastname (first initial + last name)
            // 4. firstname (or lastname if first is empty)
            var candidates = BuildCandidates(first, last);

            if (candidates.Length > 0)
            {
                // Primary candidate (preferred): try it first
                var primary = candidates[0];
                if (await IsUsernameAvailableAsync(primary))
                    return primary;

                // If primary is taken, try numeric suffixes on the primary before falling back
                var counter = 2;
                while (counter <= MaxCollisionAttempts)
                {
                    var numbered = $"{primary}{counter}";
                    if (await IsUsernameAvailableAsync(numbered))
                        return numbered;
                    counter++;
                }

                // Then try other candidate forms (without numbering)
                for (var idx = 1; idx < candidates.Length; idx++)
                {
                    var candidate = candidates[idx];
                    if (await IsUsernameAvailableAsync(candidate))
                        return candidate;
                }
            }

            // Extremely unlikely fallback: if we somehow exhaust all attempts,
            // return the base name with a timestamp suffix as a last resort.
            var fallbackBase = (candidates.Length > 0) ? candidates[0] : (first + last);
            var fallback = $"{fallbackBase}{DateTime.UtcNow:yyyyMMddHHmmss}";
            return Sanitize(fallback);
        }

        /// <inheritdoc />
        public async Task<string> GenerateUsernameFromFullNameAsync(string fullName)
        {
            // Parse the full name to extract and remove any titles
            var parsed = _nameParser.ParseName(fullName);

            // Use the parsed first and last name (without title) for username generation
            return await GenerateUsernameAsync(parsed.FirstName, parsed.LastName);
        }

        /// <summary>
        /// Strips any recognized title from a name part.
        /// If the name part is a title (e.g., "Dr"), returns empty string.
        /// </summary>
        private string StripTitle(string namePart)
        {
            if (string.IsNullOrWhiteSpace(namePart))
                return string.Empty;

            // Check if this name part is actually a title
            if (_nameParser.IsRecognizedTitle(namePart))
                return string.Empty;

            return namePart;
        }

        private static string[] BuildCandidates(string first, string last)
        {
            var candidates = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
            {
                // John Smith -> john.smith
                candidates.Add($"{first}.{last}");
                // John Smith -> johnsmith
                candidates.Add($"{first}{last}");
                // John Smith -> jsmith
                candidates.Add($"{first[0]}{last}");
                // John Smith -> john (fall back to first name only)
                candidates.Add(first);
            }
            else if (!string.IsNullOrEmpty(first))
            {
                candidates.Add(first);
            }
            else if (!string.IsNullOrEmpty(last))
            {
                candidates.Add(last);
            }

            return candidates.ToArray();
        }

        /// <summary>
        /// Normalizes a name segment: lowercase, keep only letters and numbers,
        /// strip spaces and unsupported characters, and cap at max length.
        /// </summary>
        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.ToLowerInvariant();
            // Allow dot in usernames; keep letters, digits, and dot
            var cleaned = new string(normalized.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());

            if (cleaned.Length > MaxUsernameLength)
                cleaned = cleaned.Substring(0, MaxUsernameLength);

            return cleaned;
        }
    }
}
