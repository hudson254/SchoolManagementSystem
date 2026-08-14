using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SMS.Application.Services
{
    /// <summary>
    /// Server-side password policy enforcement.
    ///
    /// This service is the authoritative source of truth for password rules.
    /// Client-side validation is a convenience only; every password is
    /// re-validated here (and rejected) before an account can be created.
    ///
    /// Storage notes:
    ///  - Passwords are NEVER logged, persisted in plaintext, or stored in
    ///    browser storage. Registration uses ASP.NET Identity's
    ///    PasswordHasher (PBKDF2 with a unique per-user random salt).
    ///  - Argon2id can be swapped in by replacing the registered
    ///    IPasswordHasher<User> implementation (see Program.cs).
    /// </summary>
    public interface IPasswordPolicyService
    {
        /// <summary>
        /// Validates a password against the full policy.
        /// Returns an empty collection when the password is acceptable.
        /// </summary>
        IReadOnlyCollection<string> Validate(string password, PasswordPolicyContext? context = null);

        /// <summary>
        /// Estimates the Shannon entropy (bits) of a password using the
        /// character-pool model: entropy = length * log2(poolSize).
        /// </summary>
        double EstimateEntropy(string password);
    }

    public sealed class PasswordPolicyContext
    {
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Organization { get; set; }
        public string? SchoolName { get; set; }
    }

    public sealed class PasswordPolicyService : IPasswordPolicyService
    {
        private const int DefaultMinLength = 8;
        private const int RecommendedMinLength = 12;
        private const double MinimumEntropy = 40.0;

        private static readonly HashSet<string> CommonBlacklist = new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "admin",
            "qwerty",
            "12345678",
            "abc123",
            "letmein",
            "welcome",
            "monkey",
            "dragon",
            "football",
            "baseball",
            "iloveyou",
            "trustno1",
            "sunshine",
            "princess",
            "master",
            "login",
            "passw0rd",
            "123456789",
            "1234567890",
            "password1",
            "qwerty123",
            "11111111"
        };

        private static readonly string[] KeyboardPatterns =
        {
            "qwerty",
            "qwertyuiop",
            "asdfghjkl",
            "zxcvbnm",
            "poiuyt",
            "mnbvcxz",
            "123456",
            "1234567",
            "12345678",
            "123456789",
            "1234567890",
            "abcdef",
            "abcdefg"
        };

        private const string DefaultSchoolName = "schoolmanagement";

        private static readonly Regex UpperRegex = new("[A-Z]", RegexOptions.Compiled);
        private static readonly Regex LowerRegex = new("[a-z]", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new("[0-9]", RegexOptions.Compiled);
        private static readonly Regex SpecialRegex = new(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?~`]", RegexOptions.Compiled);

        public IReadOnlyCollection<string> Validate(string password, PasswordPolicyContext? context = null)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required");
                return errors;
            }

            // Required rules
            if (password.Length < DefaultMinLength)
                errors.Add("Password must be at least 8 characters");

            if (!UpperRegex.IsMatch(password))
                errors.Add("Password must contain an uppercase letter");

            if (!LowerRegex.IsMatch(password))
                errors.Add("Password must contain a lowercase letter");

            if (!NumberRegex.IsMatch(password))
                errors.Add("Password must contain a number");

            if (!SpecialRegex.IsMatch(password))
                errors.Add("Password must contain at least one special character");

            // Optional recommendation (not a hard failure, but surfaced to the client)
            if (password.Length < RecommendedMinLength)
                errors.Add("Consider using 12 or more characters for stronger security");

            // Common blacklist
            if (CommonBlacklist.Contains(password))
                errors.Add("This password is too common and easy to guess");

            // Keyboard patterns
            var lower = password.ToLowerInvariant();
            if (KeyboardPatterns.Any(lower.Contains))
                errors.Add("Password contains an easy keyboard pattern");

            // Repeated sequences (e.g. "aaaa", "ababab", "123123")
            if (HasRepeatedSequence(password))
                errors.Add("Password contains repeated sequences");

            // School name / system tokens
            var schoolName = string.IsNullOrWhiteSpace(context?.SchoolName)
                ? DefaultSchoolName
                : context.SchoolName.ToLowerInvariant();
            var forbiddenTokens = new[] { schoolName, "school", "management", "system", "sms" };
            if (forbiddenTokens.Any(t => lower.Contains(t)))
                errors.Add("Password must not contain the school name");

            // Personal information
            var personalValues = new[]
            {
                context?.Email,
                context?.Username,
                context?.FirstName,
                context?.LastName,
                context?.Organization
            }
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.ToLowerInvariant())
            .Where(v => v.Length >= 3);

            foreach (var value in personalValues)
            {
                if (lower.Contains(value))
                {
                    errors.Add("Password must not contain your personal information");
                    break;
                }
            }

            // Entropy floor: reject trivially guessable passwords
            if (password.Length >= DefaultMinLength && EstimateEntropy(password) < MinimumEntropy)
                errors.Add("Password is too weak: low entropy");

            return errors;
        }

        public double EstimateEntropy(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            double poolSize = 0;
            if (LowerRegex.IsMatch(password)) poolSize += 26;
            if (UpperRegex.IsMatch(password)) poolSize += 26;
            if (NumberRegex.IsMatch(password)) poolSize += 10;
            if (SpecialRegex.IsMatch(password)) poolSize += 33;

            if (poolSize == 0)
                return 0;

            return password.Length * Math.Log2(poolSize);
        }

        private static bool HasRepeatedSequence(string password)
        {
            var lower = password.ToLowerInvariant();
            for (var len = 2; len <= lower.Length / 2; len++)
            {
                for (var i = 0; i + len * 2 <= lower.Length; i++)
                {
                    var first = lower.Substring(i, len);
                    var second = lower.Substring(i + len, len);
                    if (first == second)
                        return true;
                }
            }
            return false;
        }
    }
}
