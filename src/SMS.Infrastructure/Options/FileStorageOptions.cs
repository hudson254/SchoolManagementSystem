namespace SMS.Infrastructure.Options
{
    public class FileStorageOptions
    {
        public string Path { get; set; } = "uploads";
        public int MaxFileSizeMB { get; set; } = 10;
        public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
    }
}

