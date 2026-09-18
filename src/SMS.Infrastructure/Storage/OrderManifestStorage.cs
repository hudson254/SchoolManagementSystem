using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Storage
{
    /// <summary>
    /// Tenant-segmented filesystem storage for OMS order manifests, backed by the
    /// existing <c>OrderManifestStorage:*</c> configuration keys.
    ///
    /// Safety model (Phase 2 foundation):
    ///  - all paths are generated server-side; client-supplied paths are never used;
    ///  - storage paths must be exactly "{tenantId:N}/{fileName}" and resolve
    ///    inside the configured base directory (path traversal rejected);
    ///  - files are written atomically (temp file + move on the same volume);
    ///  - size limit and controlled extension are enforced;
    ///  - downloads go through authorized API endpoints only (never nginx).
    /// </summary>
    public class OrderManifestStorage : IOrderManifestStorage
    {
        private readonly OrderManifestStorageOptions _options;
        private readonly ILogger<OrderManifestStorage> _logger;

        public OrderManifestStorage(IOptions<OrderManifestStorageOptions> options, ILogger<OrderManifestStorage> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsEnabled => _options.Enabled;

        /// <inheritdoc />
        public async Task<string> StoreAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                throw new InvalidOperationException("Order manifest storage is disabled (OrderManifestStorage:Enabled=false).");
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var tenantRoot = GetTenantRoot(tenantId);
            var safeName = Path.GetFileName(fileName ?? throw new ArgumentException("File name is required.", nameof(fileName)));
            if (string.IsNullOrWhiteSpace(safeName))
            {
                throw new ArgumentException("File name must not be empty.", nameof(fileName));
            }

            var expectedExtension = NormalizeExtension(_options.FileExtension);
            if (!string.Equals(Path.GetExtension(safeName), expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Manifest file extension must be '{expectedExtension}'.", nameof(fileName));
            }

            Directory.CreateDirectory(tenantRoot);
            var finalPath = Path.Combine(tenantRoot, safeName);
            if (File.Exists(finalPath))
            {
                throw new IOException($"Manifest file already exists: {safeName}");
            }

            var maxBytes = _options.MaxFileSizeMB * 1024L * 1024L;
            var tempPath = Path.Combine(tenantRoot, $".{Guid.NewGuid():N}.tmp");
            try
            {
                long written = 0;
                await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        written += read;
                        if (written > maxBytes)
                        {
                            throw new InvalidOperationException(
                                $"Manifest file exceeds the configured maximum size of {_options.MaxFileSizeMB} MB.");
                        }

                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }
                }

                // Atomic publish: rename within the same directory/volume.
                File.Move(tempPath, finalPath);

                _logger.LogInformation(
                    "Stored order manifest {FileName} ({Bytes} bytes) for tenant {TenantId}",
                    safeName, written, tenantId);

                return $"{tenantId:N}/{safeName}";
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        /// <inheritdoc />
        public Task<Stream?> OpenReadAsync(Guid tenantId, string storagePath, CancellationToken cancellationToken = default)
        {
            var path = ResolveSafeStoragePath(tenantId, storagePath);
            if (!File.Exists(path))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(stream);
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(Guid tenantId, string storagePath, CancellationToken cancellationToken = default)
        {
            var path = ResolveSafeStoragePath(tenantId, storagePath);
            return Task.FromResult(File.Exists(path));
        }

        /// <inheritdoc />
        public Task DeleteAsync(Guid tenantId, string storagePath, CancellationToken cancellationToken = default)
        {
            var path = ResolveSafeStoragePath(tenantId, storagePath);
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogInformation("Deleted order manifest {StoragePath}", storagePath);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<int> ApplyRetentionAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return Task.FromResult(0);
            }

            var basePath = GetBasePath();
            if (!Directory.Exists(basePath))
            {
                return Task.FromResult(0);
            }

            var cutoff = DateTime.UtcNow - TimeSpan.FromDays(Math.Max(0, _options.RetentionDays));
            var deleted = 0;
            foreach (var tenantDir in Directory.EnumerateDirectories(basePath))
            {
                foreach (var file in Directory.EnumerateFiles(tenantDir))
                {
                    var info = new FileInfo(file);
                    if (info.LastWriteTimeUtc < cutoff)
                    {
                        info.Delete();
                        deleted++;
                    }
                }
            }

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Order manifest retention deleted {Count} file(s) older than {Cutoff:o} (RetentionDays={RetentionDays})",
                    deleted, cutoff, _options.RetentionDays);
            }

            return Task.FromResult(deleted);
        }

        private string GetBasePath()
        {
            if (string.IsNullOrWhiteSpace(_options.BasePath))
            {
                throw new InvalidOperationException("OrderManifestStorage:BasePath is not configured.");
            }

            return Path.GetFullPath(_options.BasePath);
        }

        private string GetTenantRoot(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
            }

            var basePath = GetBasePath();
            var tenantRoot = Path.GetFullPath(Path.Combine(basePath, tenantId.ToString("N")));
            var basePrefix = basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!tenantRoot.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Tenant storage root resolves outside the configured manifest base directory.");
            }

            return tenantRoot;
        }

        /// <summary>
        /// Validates a storage-relative path for the given tenant. Only the exact
        /// two-segment form "{tenantId:N}/{fileName}" is accepted, so nested
        /// paths, sibling-directory tricks and traversal attempts all fail.
        /// </summary>
        private string ResolveSafeStoragePath(Guid tenantId, string storagePath)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("Storage path must not be empty.", nameof(storagePath));
            }

            if (Path.IsPathRooted(storagePath))
            {
                throw new UnauthorizedAccessException("Absolute storage paths are not permitted.");
            }

            var basePath = GetBasePath();
            var tenantSegment = tenantId.ToString("N");
            var parts = storagePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !string.Equals(parts[0], tenantSegment, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Storage path '{storagePath}' does not belong to tenant {tenantId}.");
            }

            var fullPath = Path.GetFullPath(Path.Combine(basePath, parts[0], parts[1]));
            var basePrefix = basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Storage path '{storagePath}' resolves outside the configured manifest base directory.");
            }

            return fullPath;
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return string.Empty;
            }

            extension = extension.Trim();
            return extension.StartsWith(".", StringComparison.Ordinal) ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        }
    }
}

