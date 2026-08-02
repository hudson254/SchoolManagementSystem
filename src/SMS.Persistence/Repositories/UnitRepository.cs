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
    public class UnitRepository : BaseRepository<Unit>, IUnitRepository
    {
        public UnitRepository(ApplicationDbContext context, ILogger<UnitRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Unit>> GetUnitsByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(u => u.CourseId == courseId && !u.IsDeleted).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Unit>> GetUnitsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(u => u.CourseId == courseId && !u.IsDeleted).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Unit>> GetUnitsBySemesterAsync(int semester, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(u => u.Semester == semester && !u.IsDeleted).ToListAsync(cancellationToken);
        }

        public async Task<Unit> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Code == code && !u.IsDeleted, cancellationToken);
        }

        public async Task<Unit> GetUnitWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Course)
                .Include(u => u.Grades)
                .Include(u => u.Assignments)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Unit>> GetUnitsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            Guid? courseId,
            int? semester,
            bool? isActive,
            string sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(u => !u.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Code.Contains(searchTerm));
            if (courseId.HasValue)
                query = query.Where(u => u.CourseId == courseId.Value);
            if (semester.HasValue)
                query = query.Where(u => u.Semester == semester.Value);
            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            query = sortBy?.ToLower() switch
            {
                "name" => sortDescending ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
                "code" => sortDescending ? query.OrderByDescending(u => u.Code) : query.OrderBy(u => u.Code),
                "credits" => sortDescending ? query.OrderByDescending(u => u.Credits) : query.OrderBy(u => u.Credits),
                _ => query.OrderBy(u => u.Name)
            };

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        }

        public async Task<int> CountUnitsAsync(
            string? searchTerm,
            Guid? courseId,
            int? semester,
            bool? isActive,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(u => !u.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Code.Contains(searchTerm));
            if (courseId.HasValue)
                query = query.Where(u => u.CourseId == courseId.Value);
            if (semester.HasValue)
                query = query.Where(u => u.Semester == semester.Value);
            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query.CountAsync(cancellationToken);
        }
    }
}

