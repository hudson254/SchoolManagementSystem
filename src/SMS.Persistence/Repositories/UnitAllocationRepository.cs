using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class UnitAllocationRepository : BaseRepository<UnitAllocation>, IUnitAllocationRepository
    {
        public UnitAllocationRepository(ApplicationDbContext context, ILogger<UnitAllocationRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<UnitAllocation>> GetByLecturerAsync(Guid lecturerId)
        {
            return await _dbSet
                .Where(u => u.LecturerId == lecturerId && !u.IsDeleted)
                .OrderByDescending(u => u.AllocationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<UnitAllocation>> GetByUnitAsync(Guid unitId)
        {
            return await _dbSet
                .Where(u => u.UnitId == unitId && !u.IsDeleted)
                .OrderByDescending(u => u.AllocationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<UnitAllocation>> GetBySemesterAsync(Guid semesterId)
        {
            return await _dbSet
                .Where(u => u.SemesterId == semesterId && !u.IsDeleted)
                .OrderByDescending(u => u.AllocationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<UnitAllocation>> GetByLecturerAndSemesterAsync(Guid lecturerId, Guid semesterId)
        {
            return await _dbSet
                .Where(u => u.LecturerId == lecturerId && u.SemesterId == semesterId && !u.IsDeleted)
                .OrderByDescending(u => u.AllocationDate)
                .ToListAsync();
        }

        public async Task<bool> IsLecturerAllocatedAsync(Guid lecturerId, Guid unitId, Guid semesterId)
        {
            return await _dbSet.AnyAsync(u =>
                u.LecturerId == lecturerId &&
                u.UnitId == unitId &&
                u.SemesterId == semesterId &&
                u.Status == "Active" &&
                !u.IsDeleted);
        }

        public async Task<int> GetAllocationCountByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(u =>
                u.LecturerId == lecturerId &&
                u.Status == "Active" &&
                !u.IsDeleted, cancellationToken);
        }
    }
}
