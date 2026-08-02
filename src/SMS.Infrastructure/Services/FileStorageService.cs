using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;

namespace SMS.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageOptions _options;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(
            IOptions<FileStorageOptions> options,
            ILogger<FileStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(byte[] content, string fileName, string container)
        {
            try
            {
                var basePath = Path.Combine(_options.Path, container);
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
                var filePath = Path.Combine(basePath, safeFileName);

                await File.WriteAllBytesAsync(filePath, content);

                _logger.LogInformation("File saved: {FilePath}", filePath);
                return Path.Combine(container, safeFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file: {FileName}", fileName);
                throw;
            }
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_options.Path, filePath);
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found: {FilePath}", fullPath);
                    return Array.Empty<byte>();
                }

                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_options.Path, filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted: {FilePath}", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                throw;
            }
        }

        public bool FileExists(string filePath)
        {
            var fullPath = Path.Combine(_options.Path, filePath);
            return File.Exists(fullPath);
        }

        public async Task<string> GetFileUrlAsync(string filePath)
        {
            return $"/api/files/{filePath.Replace("\\", "/")}";
        }

        public bool IsValidFileExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _options.AllowedExtensions.Contains(extension);
        }

        public bool IsFileSizeValid(long fileSize)
        {
            return fileSize <= _options.MaxFileSizeMB * 1024 * 1024;
        }

        public string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant();
        }

        public string GetContentType(string fileName)
        {
            var extension = GetFileExtension(fileName);
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}