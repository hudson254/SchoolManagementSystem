namespace SMS.Infrastructure.Options
{
    /// <summary>
    /// Binds the existing <c>OrderManifestStorage</c> configuration section
    /// (compose: <c>OrderManifestStorage__*</c>; env: ORDER_MANIFEST_*). Defaults
    /// are the documented production contract — do not change without updating
    /// Documentation/OMS and the compose files.
    /// </summary>
    public class OrderManifestStorageOptions
    {
        public const string SectionName = "OrderManifestStorage";

        public bool Enabled { get; set; } = true;

        /// <summary>Base directory inside the container (the persistent /app/data volume).</summary>
        public string BasePath { get; set; } = "/app/data/order-manifests";

        /// <summary>Prefix for generated manifest file names.</summary>
        public string FilePrefix { get; set; } = "order-manifest-";

        /// <summary>Controlled file extension for manifest files.</summary>
        public string FileExtension { get; set; } = ".json";

        /// <summary>Retention window in days for generated manifest files.</summary>
        public int RetentionDays { get; set; } = 30;

        /// <summary>Maximum accepted manifest file size (defence against oversized writes).</summary>
        public long MaxFileSizeMB { get; set; } = 10;
    }
}
