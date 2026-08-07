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
    /// </summary>
    public class UsernameGenerator : IUsernameGenerator
    {
        private const int MaxUsernameLength = 50;
        private static readonly Regex ValidUsernameRegex = new Regex("^[a-z0-9]+$", RegexOptions.Compiled);

        private readonly IUserManagerService _userManager;

        public UsernameGenerator(IUserManagerService userManager)
        {
            _userManager = userManager;
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
            var first = Sanitize(firstName ?? string.Empty);
            var last = Sanitize(lastName ?? string.Empty);

            if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
                throw new ArgumentException("At least one of first name or last name is required.");

            // Candidate priority:
            // 1. firstname.lastname
            // 2. firstnamelastname
            // 3. flastname (first initial + last name)
            // 4. firstname (or lastname if first is empty)
            var candidates = BuildCandidates(first, last);

            foreach (var candidate in candidates)
            {
                if (await IsUsernameAvailableAsync(candidate))
                    return candidate;
            }

            // All simple variants are taken — append an incrementing number.
            // Start from 2 because the base form was already tried as a candidate.
            var baseName = candidates.LastOrDefault() ?? (first + last);
            var counter = 2;
            while (true)
            {
                var candidate = $"{baseName}{counter}";
                if (await IsUsernameAvailableAsync(candidate))
                    return candidate;
                counter++;
            }
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
            var cleaned = new string(normalized.Where(char.IsLetterOrDigit).ToArray());

            if (cleaned.Length > MaxUsernameLength)
                cleaned = cleaned.Substring(0, MaxUsernameLength);

            return cleaned;
        }
    }
}
