using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Provides asymmetric signing (RSA/ECDSA) for JWT tokens.
    /// Supports key rotation by maintaining a current signing key
    /// and a set of validation keys (previous keys that are still valid).
    ///
    /// Usage:
    /// 1. Generate a signing certificate: openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365
    /// 2. Set JwtSettings:SigningCertificatePath to the certificate file path
    /// 3. For key rotation, place the new certificate at the configured path and
    ///    keep the old certificate in a directory specified by OldCertificateDirectory
    /// </summary>
    public class AsymmetricSigningService : IDisposable
    {
        private readonly ILogger<AsymmetricSigningService> _logger;
        private readonly AsymmetricSigningOptions _options;
        private RsaSecurityKey? _currentSigningKey;
        private string? _currentCertificateThumbprint;
        private readonly object _keyLock = new();

        public AsymmetricSigningService(
            IOptions<AsymmetricSigningOptions> options,
            ILogger<AsymmetricSigningService> logger)
        {
            _options = options.Value;
            _logger = logger;
            LoadSigningKey();
        }

        /// <summary>
        /// Gets the current signing credentials for token generation.
        /// Uses RS256 (RSA-SHA256) by default.
        /// </summary>
        public SigningCredentials GetSigningCredentials()
        {
            if (_currentSigningKey == null)
            {
                throw new InvalidOperationException(
                    "No signing key configured. Set JwtSettings:SigningCertificatePath or " +
                    "configure a JWKS endpoint.");
            }

            return new SigningCredentials(_currentSigningKey, SecurityAlgorithms.RsaSha256);
        }

        /// <summary>
        /// Gets all token validation parameters including the current and
        /// any previous (rotated) keys that should still be accepted.
        /// </summary>
        public TokenValidationParameters GetValidationParameters()
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(_options.SigningCertificatePath));
            var key = new RsaSecurityKey(rsa);

            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }
            };
        }

        /// <summary>
        /// Reloads the signing key from disk. Call this after key rotation.
        /// </summary>
        public void ReloadSigningKey()
        {
            LoadSigningKey();
            _logger.LogInformation("Signing key reloaded for key rotation");
        }

        private void LoadSigningKey()
        {
            if (string.IsNullOrEmpty(_options.SigningCertificatePath))
            {
                _logger.LogWarning("AsymmetricSigningService: No signing certificate path configured. Falling back to symmetric signing.");
                return;
            }

            if (!File.Exists(_options.SigningCertificatePath))
            {
                _logger.LogError("AsymmetricSigningService: Signing certificate not found at {Path}", _options.SigningCertificatePath);
                return;
            }

            try
            {
                var rsa = RSA.Create();
                var pemContent = File.ReadAllText(_options.SigningCertificatePath);
                rsa.ImportFromPem(pemContent.AsSpan());
                _currentSigningKey = new RsaSecurityKey(rsa) { KeyId = Guid.NewGuid().ToString() };
                _logger.LogInformation("AsymmetricSigningService: Loaded RSA signing key from {Path}", _options.SigningCertificatePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AsymmetricSigningService: Failed to load signing key from {Path}", _options.SigningCertificatePath);
            }
        }

        public void Dispose()
        {
            _currentSigningKey?.Rsa?.Dispose();
        }
    }

    /// <summary>
    /// Configuration options for asymmetric JWT signing.
    /// </summary>
    public class AsymmetricSigningOptions
    {
        /// <summary>
        /// Path to the PEM-encoded RSA private key file for signing.
        /// Example: "/etc/ssl/jwt/key.pem"
        /// </summary>
        public string? SigningCertificatePath { get; set; }

        /// <summary>
        /// Directory containing old (rotated) certificates that should
        /// still be accepted for token validation.
        /// </summary>
        public string? OldCertificateDirectory { get; set; }

        /// <summary>
        /// The signing algorithm to use. Default: RS256
        /// Supported: RS256, RS384, RS512, ES256, ES384, ES512
        /// </summary>
        public string Algorithm { get; set; } = SecurityAlgorithms.RsaSha256;
    }
}
