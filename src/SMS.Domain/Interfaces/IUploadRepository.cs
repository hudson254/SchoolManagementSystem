using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Repository for managing uploaded file metadata records.
    /// </summary>
    public interface IUploadRepository
    {
        Task<UploadFile> GetByIdAsync(Guid id);
        Task<UploadFile> GetByHashAsync(string sha256Hash);
        Task<IEnumerable<UploadFile>> GetByCategoryAsync(UploadCategory category, int page = 1, int pageSize = 50);
        Task<IEnumerable<UploadFile>> GetByUserAsync(string userId, int page = 1, int pageSize = 50);
        Task<IEnumerable<UploadFile>> GetByCourseOfferingAsync(Guid courseOfferingId);
        Task<IEnumerable<UploadFile>> GetByUnitAsync(Guid unitId);
        Task<IEnumerable<UploadFile>> GetByAssignmentAsync(Guid assignmentId);
        Task<IEnumerable<UploadFile>> GetVersionsAsync(string originalFileName, string storagePath);
        Task<int> GetNextVersionAsync(string baseIdentifier);
        Task<IEnumerable<UploadFile>> SearchAsync(string query, int page = 1, int pageSize = 50);
        Task<long> GetTotalCountAsync();
        Task<long> GetTotalSizeBytesAsync();
        Task<Dictionary<string, long>> GetStorageStatisticsAsync();
        Task AddAsync(UploadFile uploadFile);
        Task UpdateAsync(UploadFile uploadFile);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsByHashAsync(string sha256Hash);
    }
}
