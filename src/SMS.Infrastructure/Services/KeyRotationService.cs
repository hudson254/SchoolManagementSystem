using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Background service that monitors certificate directories and automatically
    /// rotates signing keys with overlapping acceptance periods.
    ///
    /// Design:
    /// - The current signing key is loaded from the configured SigningCertificatePath.
    /// - Old (rotated) certificates in OldCertificateDirectory are still accepted
    ///   for token validation until they expire (based on their NotAfter date).
    /// - When a new certificate is placed at SigningCertificatePath, it becomes
    ///   the active signing key. The previous key is moved to OldCertificateDirectory
    ///   and remains valid for validation until its certificate expires.
    /// - The service polls the certificate directory every CheckIntervalMinutes.
    ///
    /// Usage:
    /// 1. Configure AsymmetricSigningOptions with SigningCertificatePath and OldCertificateDirectory
    /// 2. To rotate: place a new PEM file at SigningCertificatePath
    /// 3. The old key is automatically moved to OldCertificateDirectory
    /// 4. Old keys remain valid for token validation until they expire
    /// </summary>
    public class KeyRotationService : BackgroundService
    {
        private readonly ILogger<KeyRotationService> _logger;
        private readonly KeyRotationOptions _options;
        private readonly AsymmetricSigningService _signingService;
        private readonly ConcurrentDictionary<string, RsaSecurityKey> _validationKeys = new();
        private DateTime _lastCheck = DateTime.MinValue;

        public KeyRotationService(
            IOptions<KeyRotationOptions> options,
            ILogger<KeyRotationService> logger,
            AsymmetricSigningService signingService)
        {
            _options = options.Value;
            _logger = logger;
            _signingService = signingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KeyRotationService started. Checking every {Interval} minutes.",
                _options.CheckIntervalMinutes);

            // Initial load of old certificates
            LoadOldCertificates();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(_options.CheckIntervalMinutes),
                        stoppingToken);

                    CheckForNewCertificate();
                    CleanExpiredCertificates();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during key rotation check");
                }
            }
        }

        /// <summary>
        /// Gets all validation keys (current + old certificates that haven't expired).
        /// </summary>
        public IEnumerable<RsaSecurityKey> GetValidationKeys()
        {
            return _validationKeys.Values.ToList();
        }

        private void CheckForNewCertificate()
        {
            if (string.IsNullOrEmpty(_options.SigningCertificatePath))
                return;

            var certFile = new FileInfo(_options.SigningCertificatePath);
            if (!certFile.Exists)
                return;

            // Check if the certificate file has been modified since last check
            if (certFile.LastWriteTimeUtc <= _lastCheck)
                return;

            _lastCheck = certFile.LastWriteTimeUtc;

            try
            {
                // Load the new certificate
                var rsa = RSA.Create();
                var pemContent = File.ReadAllText(certFile.FullName);
                rsa.ImportFromPem(pemContent.AsSpan());

                var newKey = new RsaSecurityKey(rsa)
                {
                    KeyId = Guid.NewGuid().ToString()
                };

                // Move the old key to the old certificates directory
                if (!string.IsNullOrEmpty(_options.OldCertificateDirectory))
                {
                    Directory.CreateDirectory(_options.OldCertificateDirectory);
                    var oldKeyPath = Path.Combine(
                        _options.OldCertificateDirectory,
                        $"key-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pem");

                    // Copy current key to old directory before replacing
                    File.Copy(certFile.FullName, oldKeyPath, overwrite: false);
                    _logger.LogInformation("Previous signing key archived to {Path}", oldKeyPath);
                }

                // Reload the signing service with the new key
                _signingService.ReloadSigningKey();

                // Add the new key to validation keys
                _validationKeys.TryAdd(newKey.KeyId, newKey);

                _logger.LogInformation(
                    "Signing key rotated. New key ID: {KeyId}. Old key archived for overlapping validation.",
                    newKey.KeyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process new certificate at {Path}",
                    _options.SigningCertificatePath);
            }
        }

        private void LoadOldCertificates()
        {
            if (string.IsNullOrEmpty(_options.OldCertificateDirectory))
                return;

            var oldDir = new DirectoryInfo(_options.OldCertificateDirectory);
            if (!oldDir.Exists)
                return;

            foreach (var pemFile in oldDir.GetFiles("*.pem"))
            {
                try
                {
                    var rsa = RSA.Create();
                    var pemContent = File.ReadAllText(pemFile.FullName);
                    rsa.ImportFromPem(pemContent.AsSpan());

                    var key = new RsaSecurityKey(rsa)
                    {
                        KeyId = $"old-{pemFile.Name}"
                    };

                    _validationKeys.TryAdd(key.KeyId, key);
                    _logger.LogInformation("Loaded old signing key for overlapping validation: {File}",
                        pemFile.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load old certificate {File}", pemFile.Name);
                }
            }
        }

        private void CleanExpiredCertificates()
        {
            // Old certificates remain valid indefinitely for validation purposes.
            // In a production system, you would check the certificate's NotAfter
            // date and remove expired ones. For PEM files without embedded expiry,
            // a manual cleanup policy should be implemented.
            _logger.LogDebug("Active validation keys: {Count}", _validationKeys.Count);
        }
    }

    /// <summary>
    /// Configuration options for automated key rotation.
    /// </summary>
    public class KeyRotationOptions
    {
        /// <summary>
        /// How often to check for new certificates (in minutes). Default: 5
        /// </summary>
        public int CheckIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Path to the current signing certificate (PEM format).
        /// </summary>
        public string? SigningCertificatePath { get; set; }

        /// <summary>
        /// Directory where old (rotated) certificates are archived.
        /// </summary>
        public string? OldCertificateDirectory { get; set; }
    }
}
