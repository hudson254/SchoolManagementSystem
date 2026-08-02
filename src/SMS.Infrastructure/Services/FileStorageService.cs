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

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string container = null)
        {
            try
            {
                var basePath = _options.Path ?? "uploads";
                var containerPath = string.IsNullOrEmpty(container) ? basePath : Path.Combine(basePath, container);

                if (!Directory.Exists(containerPath))
                {
                    Directory.CreateDirectory(containerPath);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var filePath = Path.Combine(containerPath, uniqueFileName);

                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                _logger.LogInformation("File uploaded: {FileName} to {Container}", fileName, container ?? "root");
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
                var basePath = _options.Path ?? "uploads";
                var fullPath = Path.Combine(basePath, filePath.Replace('/', '\\'));

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
                var basePath = _options.Path ?? "uploads";
                var fullPath = Path.Combine(basePath, filePath.Replace('/', '\\'));

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
