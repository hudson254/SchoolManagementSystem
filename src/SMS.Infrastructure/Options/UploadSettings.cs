using System.Collections.Generic;

namespace SMS.Infrastructure.Options
{
    /// <summary>
    /// Enterprise-grade upload configuration settings.
    /// All upload validation rules are driven by these settings.
    /// </summary>
    public class UploadSettings
    {
        /// <summary>
        /// Configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "UploadSettings";

        /// <summary>
        /// Base storage path for all uploads
        /// </summary>
        public string StoragePath { get; set; } = "uploads";

        /// <summary>
        /// Maximum filename length (excluding extension)
        /// </summary>
        public int MaxFilenameLength { get; set; } = 100;

        /// <summary>
        /// Whether to enable malware/virus scanning
        /// </summary>
        public bool EnableMalwareScanning { get; set; } = false;

        /// <summary>
        /// Whether to enable duplicate detection via SHA-256
        /// </summary>
        public bool EnableDuplicateDetection { get; set; } = true;

        /// <summary>
        /// Allowed document file extensions
        /// </summary>
        public HashSet<string> AllowedDocumentExtensions { get; set; } = new()
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx",
            ".xls", ".xlsx", ".odt", ".odp", ".ods",
            ".rtf", ".txt", ".csv"
        };

        /// <summary>
        /// Allowed image file extensions
        /// </summary>
        public HashSet<string> AllowedImageExtensions { get; set; } = new()
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        /// <summary>
        /// Allowed archive file extensions (only if explicitly enabled)
        /// </summary>
        public HashSet<string> AllowedArchiveExtensions { get; set; } = new()
        {
            ".zip"
        };

        /// <summary>
        /// Extensions that are explicitly blocked/forbidden
        /// </summary>
        public HashSet<string> BlockedExtensions { get; set; } = new()
        {
            ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".scr",
            ".msi", ".apk", ".com", ".jar", ".js", ".php", ".asp",
            ".aspx", ".py", ".sh", ".app", ".gadget", ".hta",
            ".ins", ".isp", ".its", ".jse", ".ksh", ".lnk",
            ".mad", ".maf", ".mag", ".mam", ".maq", ".mar",
            ".mas", ".mat", ".mau", ".mav", ".maw", ".mda",
            ".mdb", ".mde", ".mdt", ".mdw", ".mdz", ".msc",
            ".msh", ".msh1", ".msh2", ".mshxml", ".msh1xml", ".msh2xml",
            ".msp", ".mst", ".ops", ".pcd", ".pif", ".pl",
            ".plg", ".prf", ".prg", ".pst", ".reg", ".scf",
            ".sct", ".shb", ".shs", ".tmp", ".url", ".vb",
            ".vbe", ".vbs", ".vsmacros", ".vsw", ".ws", ".wsc",
            ".wsf", ".wsh", ".xsl"
        };

        /// <summary>
        /// Maximum file sizes per category (in MB)
        /// </summary>
        public Dictionary<string, int> MaxFileSizesMB { get; set; } = new()
        {
            ["StudentAssignment"] = 20,
            ["LecturerNotes"] = 50,
            ["CourseResources"] = 100,
            ["ProfileImage"] = 5,
            ["AdminDocument"] = 50,
            ["Dataset"] = 100,
            ["CertificateTemplate"] = 10,
            ["AssignmentBrief"] = 50,
            ["SupportingDocument"] = 20,
            ["Default"] = 10
        };
    }
}
