using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Tenant-segmented, generated-name storage for OMS order manifests.
    /// Storage paths handled by this abstraction are ALWAYS relative to the
    /// configured manifest root and always include the tenant segment — arbitrary
    /// filesystem paths from clients are never accepted. Backed by the existing
    /// <c>OrderManifestStorage:*</c> configuration keys.
    /// </summary>
    public interface IOrderManifestStorage
    {
        /// <summary>Whether manifest storage is enabled via configuration.</summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Stores manifest content under the tenant's segment using the supplied
        /// generated file name (already collision-resistant; caller-owned format).
        /// Returns the storage-relative path (e.g. "{tenantN}/{fileName}") that
        /// must be persisted with the <c>OrderManifest</c> metadata record.
        /// Throws if the file already exists or exceeds the configured maximum size.
        /// </summary>
        Task<string> StoreAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens the stored manifest for reading, or null when it does not exist.
        /// The caller owns and must dispose the returned stream.
        /// </summary>
        Task<Stream?> OpenReadAsync(Guid tenantId, string storagePath, CancellationToken cancellationToken = default);

        /// <summary>Returns whether the manifest file exists for the given tenant.</summary>
        Task<bool> ExistsAsync(Guid tenantId, string storagePath, CancellationToken cancellationToken = default);

        /// <summary>Deletes the manifest file for the given tenant (no-op when absent).</summary>
        Task DeleteAsync(Guid tenantId, string storagePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes manifest files older than the configured retention window
        /// across all tenant segments. Returns the number of files deleted.
        /// </summary>
        Task<int> ApplyRetentionAsync(CancellationToken cancellationToken = default);
    }
}
