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
    public class LecturerRepository : BaseRepository<Lecturer>, ILecturerRepository
    {
        public LecturerRepository(ApplicationDbContext context, ILogger<LecturerRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Lecturer>> GetLecturersByDepartmentAsync(Guid departmentId)
        {
            return await _dbSet.Where(l => l.DepartmentId == departmentId && !l.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Lecturer>> GetActiveLecturersAsync()
        {
            return await _dbSet.Where(l => l.IsActive && !l.IsDeleted).ToListAsync();
        }

        public async Task<Lecturer> GetLecturerByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(l => l.Email == email && !l.IsDeleted);
        }

        public async Task<int> CountLecturersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(l => !l.IsDeleted, cancellationToken);
        }

        public async Task<Lecturer> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(l => l.UserId == userId.ToString() && !l.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Guid>> GetTaughtUnitIdsAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            var result = new HashSet<Guid>();

            // 1. Direct unit allocations (active status)
            var allocatedUnitIds = await _context.Set<UnitAllocation>()
                .Where(u => u.LecturerId == lecturerId && u.Status == "Active" && !u.IsDeleted)
                .Select(u => u.UnitId)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in allocatedUnitIds)
            {
                result.Add(id);
            }

            // 2. Course-offering lecturer assignments -> offering units
            var offeringIds = await _context.Set<CourseOfferingLecturer>()
                .Where(l => l.LecturerId == lecturerId && l.IsActive && !l.IsDeleted)
                .Select(l => l.CourseOfferingId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (offeringIds.Count > 0)
            {
                var offeringUnitIds = await _context.Set<CourseOfferingUnit>()
                    .Where(u => u.UnitId != null && offeringIds.Contains(u.CourseOfferingId) && u.IsActive && !u.IsDeleted)
                    .Select(u => u.UnitId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                foreach (var id in offeringUnitIds)
                {
                    result.Add(id);
                }
            }

            return result;
        }
    }
}

