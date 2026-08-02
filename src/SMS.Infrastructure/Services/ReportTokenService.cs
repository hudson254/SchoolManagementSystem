using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Service for generating cryptographically secure report tokens and Report IDs.
    /// </summary>
    public interface IReportTokenService
    {
        /// <summary>
        /// Generates a globally unique Report ID in format MTS-YYYYMMDD-XXXXXXXX.
        /// </summary>
        string GenerateReportId();

        /// <summary>
        /// Generates a cryptographically secure verification token.
        /// Token is non-predictable and resistant to brute-force attacks.
        /// </summary>
        /// <returns>Base64-encoded secure token</returns>
        string GenerateVerificationToken();

        /// <summary>
        /// Validates that a token is in the correct format.
        /// </summary>
        bool IsValidTokenFormat(string token);
    }

    public class ReportTokenService : IReportTokenService
    {
        private readonly ILogger<ReportTokenService> _logger;

        public ReportTokenService(ILogger<ReportTokenService> logger)
        {
            _logger = logger;
        }

        public string GenerateReportId()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");

            // Generate a random 10-character hex suffix using cryptographic RNG
            var randomBytes = new byte[5]; // 5 bytes = 10 hex chars
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var hexPart = Convert.ToHexString(randomBytes).ToUpper();

            var reportId = $"MTS-{datePart}-{hexPart}";
            _logger.LogDebug("Generated Report ID: {ReportId}", reportId);
            return reportId;
        }

        public string GenerateVerificationToken()
        {
            // Generate 32 bytes of cryptographically random data
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            // Use URL-safe Base64 encoding (no padding)
            var token = Convert.ToBase64String(tokenBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            _logger.LogDebug("Generated verification token of length {Length}", token.Length);
            return token;
        }

        public bool IsValidTokenFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // Check if token matches expected format: URL-safe Base64 of 32 bytes = 43 chars without padding
            if (token.Length < 32 || token.Length > 64)
                return false;

            // Check for valid URL-safe Base64 characters
            foreach (var c in token)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    return false;
            }

            return true;
        }
    }
}
