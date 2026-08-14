using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Persistence.Repositories
{
    public class UploadRepository : IUploadRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UploadRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(UploadFile uploadFile)
        {
            await _dbContext.Set<UploadFile>().AddAsync(uploadFile);
        }

        public async Task DeleteAsync(Guid id)
        {
            var file = await GetByIdAsync(id);
            if (file != null)
            {
                file.SoftDelete("System");
                _dbContext.Set<UploadFile>().Update(file);
            }
        }

        public async Task<bool> ExistsByHashAsync(string sha256Hash)
        {
            return await _dbContext.Set<UploadFile>()
                .AnyAsync(f => f.Sha256Hash == sha256Hash && f.Status == "Active");
        }

        public async Task<UploadFile> GetByIdAsync(Guid id)
        {
            return await _dbContext.Set<UploadFile>()
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<UploadFile> GetByHashAsync(string sha256Hash)
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => f.Sha256Hash == sha256Hash && f.Status == "Active" && !f.IsDeleted)
                .OrderByDescending(f => f.Version)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<UploadFile>> GetByCategoryAsync(UploadCategory category, int page = 1, int pageSize = 50)
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => f.Category == category && !f.IsDeleted)
                .OrderByDescending(f => f.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<UploadFile>> GetByUserAsync(string userId, int page = 1, int pageSize = 50)
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => f.UploadedByUserId == userId && !f.IsDeleted)
                .OrderByDescending(f => f.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<UploadFile>> GetByCourseOfferingAsync(Guid courseOfferingId)
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => f.CourseOfferingId == courseOfferingId && !f.IsDeleted)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UploadFile>> GetByUnitAsync(Guid unitId)
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => f.UnitId == unitId && !f.IsDeleted)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UploadFile>> GetByAssignmentAsync(Guid assignmentId)
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => f.AssignmentId == assignmentId && !f.IsDeleted)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UploadFile>> GetVersionsAsync(string originalFileName, string storagePath)
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => f.StoragePath.StartsWith(storagePath) && !f.IsDeleted)
                .OrderBy(f => f.Version)
                .ToListAsync();
        }

        public async Task<int> GetNextVersionAsync(string baseIdentifier)
        {
            var lastVersion = await _dbContext.Set<UploadFile>()
                .Where(f => f.GeneratedFileName.StartsWith(baseIdentifier) && !f.IsDeleted)
                .OrderByDescending(f => f.Version)
                .Select(f => (int?)f.Version)
                .FirstOrDefaultAsync();

            return (lastVersion ?? 0) + 1;
        }

        public async Task<IEnumerable<UploadFile>> SearchAsync(string query, int page = 1, int pageSize = 50)
        {
            var searchTerm = query.ToLowerInvariant();
            return await _dbContext.Set<UploadFile>()
                .Where(f => !f.IsDeleted &&
                    (f.OriginalFileName.ToLower().Contains(searchTerm) ||
                     f.GeneratedFileName.ToLower().Contains(searchTerm) ||
                     f.UploadedByUsername.ToLower().Contains(searchTerm)))
                .OrderByDescending(f => f.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<long> GetTotalCountAsync()
        {
            return await _dbContext.Set<UploadFile>()
                .CountAsync(f => !f.IsDeleted);
        }

        public async Task<long> GetTotalSizeBytesAsync()
        {
            return await _dbContext.Set<UploadFile>()
                .Where(f => !f.IsDeleted)
                .SumAsync(f => (long?)f.FileSizeBytes) ?? 0;
        }

        public async Task<Dictionary<string, long>> GetStorageStatisticsAsync()
        {
            var stats = await _dbContext.Set<UploadFile>()
                .Where(f => !f.IsDeleted)
                .GroupBy(f => f.Extension)
                .Select(g => new { Extension = g.Key, TotalSize = g.Sum(f => f.FileSizeBytes), Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<string, long>();
            foreach (var stat in stats)
            {
                result[$"{stat.Extension}_count"] = stat.Count;
                result[$"{stat.Extension}_size"] = stat.TotalSize;
            }
            return result;
        }

        public async Task UpdateAsync(UploadFile uploadFile)
        {
            _dbContext.Set<UploadFile>().Update(uploadFile);
            await Task.CompletedTask;
        }
    }
}
