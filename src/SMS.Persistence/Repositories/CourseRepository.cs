using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly ApplicationDbContext _context;

        public CourseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Programmes)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Course>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .Include(c => c.Department)
                .Where(c => !c.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Course>> FindAsync(Expression<Func<Course, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .Include(c => c.Department)
                .Where(c => !c.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Course> AddAsync(Course entity, CancellationToken cancellationToken = default)
        {
            await _context.Courses.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Course>> AddRangeAsync(IEnumerable<Course> entities, CancellationToken cancellationToken = default)
        {
            await _context.Courses.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Course entity, CancellationToken cancellationToken = default)
        {
            _context.Courses.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Course entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Courses.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Course, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Courses.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Course, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Courses.Where(c => !c.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<Course?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted, cancellationToken);
        }

        public async Task<Course?> GetCourseWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.Programmes)
                    .ThenInclude(p => p.Students)
                .Include(c => c.Units)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Course>> GetCoursesByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .Include(c => c.Department)
                .Where(c => c.DepartmentId == departmentId && !c.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Course>> GetActiveCoursesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Courses
                .Include(c => c.Department)
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync(cancellationToken);
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
            var query = _context.Courses
                .Include(c => c.Department)
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c =>
                    c.Name.Contains(searchTerm) ||
                    c.Code.Contains(searchTerm) ||
                    c.Description.Contains(searchTerm));
            }

            if (departmentId.HasValue)
                query = query.Where(c => c.DepartmentId == departmentId.Value);

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            query = sortDescending
                ? query.OrderByDescending(GetSortExpression(sortBy))
                : query.OrderBy(GetSortExpression(sortBy));

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountCoursesAsync(string? searchTerm, Guid? departmentId, bool? isActive, CancellationToken cancellationToken = default)
        {
            var query = _context.Courses.Where(c => !c.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c =>
                    c.Name.Contains(searchTerm) ||
                    c.Code.Contains(searchTerm) ||
                    c.Description.Contains(searchTerm));
            }

            if (departmentId.HasValue)
                query = query.Where(c => c.DepartmentId == departmentId.Value);

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<bool> HasActiveUnitsAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .AnyAsync(u => u.CourseId == courseId && u.IsActive && !u.IsDeleted, cancellationToken);
        }

        private static Expression<Func<Course, object>> GetSortExpression(string sortBy)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "name" => c => c.Name,
                "code" => c => c.Code,
                "createddate" => c => c.CreatedDate,
                _ => c => c.CreatedDate
            };
        }
    }
}