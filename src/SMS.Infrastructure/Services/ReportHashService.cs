using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Service for computing cryptographic hashes of report content.
    /// Uses SHA-256 by default for tamper detection.
    /// </summary>
    public interface IReportHashService
    {
        /// <summary>
        /// Computes a SHA-256 hash of the report content.
        /// </summary>
        /// <param name="content">The report content bytes</param>
        /// <returns>Hex-encoded SHA-256 hash string</returns>
        string ComputeHash(byte[] content);

        /// <summary>
        /// Computes a hash using the specified algorithm.
        /// </summary>
        /// <param name="content">The report content bytes</param>
        /// <param name="algorithm">Hash algorithm name (e.g., SHA-256, SHA-384, SHA-512)</param>
        /// <returns>Hex-encoded hash string</returns>
        string ComputeHash(byte[] content, string algorithm);

        /// <summary>
        /// Validates that the content matches the expected hash.
        /// </summary>
        /// <param name="content">The report content bytes to verify</param>
        /// <param name="expectedHash">The expected hash value</param>
        /// <param name="algorithm">Hash algorithm used (default: SHA-256)</param>
        /// <returns>True if the hash matches</returns>
        bool ValidateHash(byte[] content, string expectedHash, string algorithm = "SHA-256");

        /// <summary>
        /// Gets the supported hash algorithms.
        /// </summary>
        IEnumerable<string> GetSupportedAlgorithms();
    }

    public class ReportHashService : IReportHashService
    {
        private readonly ILogger<ReportHashService> _logger;
        private static readonly HashSet<string> SupportedAlgorithms = new(StringComparer.OrdinalIgnoreCase)
        {
            "SHA-256", "SHA256", "SHA-384", "SHA384", "SHA-512", "SHA512"
        };

        public ReportHashService(ILogger<ReportHashService> logger)
        {
            _logger = logger;
        }

        public string ComputeHash(byte[] content)
        {
            return ComputeHash(content, "SHA-256");
        }

        public string ComputeHash(byte[] content, string algorithm)
        {
            if (content == null || content.Length == 0)
            {
                _logger.LogWarning("Attempted to hash empty content");
                return string.Empty;
            }

            try
            {
                var normalizedAlgorithm = NormalizeAlgorithm(algorithm);
                using (var hashAlgorithm = HashAlgorithm.Create(normalizedAlgorithm))
                {
                    if (hashAlgorithm == null)
                    {
                        _logger.LogError("Unsupported hash algorithm: {Algorithm}", algorithm);
                        throw new ArgumentException($"Unsupported hash algorithm: {algorithm}", nameof(algorithm));
                    }

                    var hashBytes = hashAlgorithm.ComputeHash(content);
                    var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

                    _logger.LogDebug("Hash computed using {Algorithm}: {HashLength} chars", algorithm, hashString.Length);
                    return hashString;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute hash using {Algorithm}", algorithm);
                throw;
            }
        }

        public bool ValidateHash(byte[] content, string expectedHash, string algorithm = "SHA-256")
        {
            if (content == null || content.Length == 0)
            {
                _logger.LogWarning("Cannot validate hash for empty content");
                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                _logger.LogWarning("Expected hash is empty");
                return false;
            }

            try
            {
                var computedHash = ComputeHash(content, algorithm);
                var isValid = string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning(
                        "Hash validation failed. Expected: {Expected}, Computed: {Computed}",
                        expectedHash[..Math.Min(expectedHash.Length, 16)] + "...",
                        computedHash[..Math.Min(computedHash.Length, 16)] + "...");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hash validation error");
                return false;
            }
        }

        public IEnumerable<string> GetSupportedAlgorithms()
        {
            return SupportedAlgorithms;
        }

        private static string NormalizeAlgorithm(string algorithm)
        {
            return algorithm.ToUpperInvariant() switch
            {
                "SHA-256" or "SHA256" => "SHA256",
                "SHA-384" or "SHA384" => "SHA384",
                "SHA-512" or "SHA512" => "SHA512",
                _ => algorithm
            };
        }
    }
}
