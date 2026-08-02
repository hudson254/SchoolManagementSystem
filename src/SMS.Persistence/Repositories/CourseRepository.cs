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
    public class CourseRepository : BaseRepository<Course>, ICourseRepository
    {
        public CourseRepository(ApplicationDbContext context, ILogger<CourseRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Course>> GetCoursesByDepartmentAsync(Guid departmentId)
        {
            return await _dbSet.Where(c => c.DepartmentId == departmentId && !c.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Course>> GetActiveCoursesAsync()
        {
            return await _dbSet.Where(c => c.IsActive && !c.IsDeleted).ToListAsync();
        }

        public async Task<Course> GetCourseWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.Department)
                .Include(c => c.Programme)
                .Include(c => c.Semester)
                .Include(c => c.Units)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        }

        public async Task<Course> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Course>> GetCoursesAsync(
            int page,
            int pageSize,
            string? searchTerm,
            Guid? departmentId,
            bool? isActive,
            string sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => !c.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => c.Name.Contains(searchTerm) || c.Code.Contains(searchTerm));
            if (departmentId.HasValue)
                query = query.Where(c => c.DepartmentId == departmentId.Value);
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            query = sortBy?.ToLower() switch
            {
                "name" => sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                "code" => sortDescending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
                "credits" => sortDescending ? query.OrderByDescending(c => c.Credits) : query.OrderBy(c => c.Credits),
                _ => query.OrderBy(c => c.Name)
            };

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        }

        public async Task<int> CountCoursesAsync(string? searchTerm, Guid? departmentId, bool? isActive, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => !c.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => c.Name.Contains(searchTerm) || c.Code.Contains(searchTerm));
            if (departmentId.HasValue)
                query = query.Where(c => c.DepartmentId == departmentId.Value);
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<bool> HasActiveUnitsAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(c => c.Id == courseId && !c.IsDeleted)
                .SelectMany(c => c.Units)
                .AnyAsync(u => u.IsActive && !u.IsDeleted, cancellationToken);
        }
    }
}

