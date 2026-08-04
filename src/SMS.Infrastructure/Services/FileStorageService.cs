using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageOptions _options;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IOptions<FileStorageOptions> options, ILogger<FileStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Resolves a user-supplied relative path to a fully-qualified path
        /// guaranteed to stay within the configured base directory.
        ///
        /// RISK-22 FIX: Rejects path-traversal attempts ("../", "..\") and
        /// absolute paths that would escape the storage root. The resolved
        /// base path is normalized (full path) and the target is required to
        /// be a descendant of it, so "..\..\secret.txt" and
        /// "C:\Windows\system.ini" cannot be read/written/deleted.
        /// </summary>
        private string ResolveSafePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Path must not be empty.", nameof(relativePath));

            var basePath = Path.GetFullPath(_options.Path ?? "uploads");

            // Normalize separators to the platform separator, then resolve.
            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(basePath, normalized));

            // The resolved path must be inside the base directory. A prefix
            // check with the trailing separator prevents "uploads-evil" from
            // being accepted as a sibling to "uploads".
            var basePrefix = basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    $"Path '{relativePath}' resolves outside the configured storage directory.");
            }

            return fullPath;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string container = null)
        {
            try
            {
                var basePath = _options.Path ?? "uploads";
                var containerPath = string.IsNullOrEmpty(container) ? basePath : Path.Combine(basePath, container);

                // Validate the container path stays inside the storage root.
                ResolveSafePath(containerPath);

                if (!Directory.Exists(containerPath))
                {
                    Directory.CreateDirectory(containerPath);
                }

                var safeFileName = Path.GetFileName(fileName);
                if (string.IsNullOrEmpty(safeFileName))
                    throw new ArgumentException("File name must not be empty.", nameof(fileName));

                var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                var filePath = Path.Combine(containerPath, uniqueFileName);

                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                _logger.LogInformation("File uploaded: {FileName} to {Container}", safeFileName, container ?? "root");
                return Path.Combine(container ?? "", uniqueFileName).Replace('\\', '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            try
            {
                var fullPath = ResolveSafePath(filePath);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(fullPath, FileMode.Open))
                {
                    await fileStream.CopyToAsync(memoryStream);
                }

                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = ResolveSafePath(filePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted: {FilePath}", filePath);
                    return await Task.FromResult(true);
                }

                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<string> GetFileUrlAsync(string filePath)
        {
            return await Task.FromResult($"/uploads/{filePath}");
        }

        public async Task<string> SaveFileAsync(byte[] fileBytes, string fileName, string container = null)
        {
            using (var stream = new MemoryStream(fileBytes))
            {
                return await UploadFileAsync(stream, fileName, container);
            }
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            using (var stream = await DownloadFileAsync(filePath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}
