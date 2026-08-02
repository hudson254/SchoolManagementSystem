using System.IO;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string container = null);
        Task<Stream> DownloadFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<string> GetFileUrlAsync(string filePath);
        Task<string> SaveFileAsync(byte[] fileBytes, string fileName, string container = null);
        Task<byte[]> GetFileAsync(string filePath);
    }
}