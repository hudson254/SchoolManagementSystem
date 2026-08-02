using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly ApplicationDbContext _context;

        public UnitRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(u => u.Course)
                .Include(u => u.Prerequisite)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(u => u.Course)
                .Where(u => !u.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Unit>> FindAsync(Expression<Func<Unit, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(u => u.Course)
                .Where(u => !u.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Unit> AddAsync(Unit entity, CancellationToken cancellationToken = default)
        {
            await _context.Units.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Unit>> AddRangeAsync(IEnumerable<Unit> entities, CancellationToken cancellationToken = default)
        {
            await _context.Units.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Unit entity, CancellationToken cancellationToken = default)
        {
            _context.Units.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Unit entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Units.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Unit, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Units.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Unit, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Units.Where(u => !u.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<Unit?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(u => u.Course)
                .FirstOrDefaultAsync(u => u.Code == code && !u.IsDeleted, cancellationToken);
        }

        public async Task<Unit?> GetUnitWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(u => u.Course)
                .Include(u => u.Prerequisite)
                .Include(u => u.Allocations)
                    .ThenInclude(a => a.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(u => u.Enrollments)
                .Include(u => u.Assignments)
                .Include(u => u.LectureNotes)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Unit>> GetUnitsByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(u => u.Course)
                .Where(u => u.CourseId == courseId && !u.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Unit>> GetActiveUnitsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Units
                .Include(u => u.Course)
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Unit>> GetUnitsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            Guid? courseId,
            bool? isActive,
            string sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Units
                .Include(u => u.Course)
                .Include(u => u.Prerequisite)
                .Where(u => !u.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u =>
                    u.Name.Contains(searchTerm) ||
                    u.Code.Contains(searchTerm) ||
                    u.Description.Contains(searchTerm));
            }

            if (courseId.HasValue)
                query = query.Where(u => u.CourseId == courseId.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            query = sortDescending
                ? query.OrderByDescending(GetSortExpression(sortBy))
                : query.OrderBy(GetSortExpression(sortBy));

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountUnitsAsync(string? searchTerm, Guid? courseId, bool? isActive, CancellationToken cancellationToken = default)
        {
            var query = _context.Units.Where(u => !u.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u =>
                    u.Name.Contains(searchTerm) ||
                    u.Code.Contains(searchTerm) ||
                    u.Description.Contains(searchTerm));
            }

            if (courseId.HasValue)
                query = query.Where(u => u.CourseId == courseId.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query.CountAsync(cancellationToken);
        }

        private static Expression<Func<Unit, object>> GetSortExpression(string sortBy)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "name" => u => u.Name,
                "code" => u => u.Code,
                "credits" => u => u.Credits,
                "createddate" => u => u.CreatedDate,
                _ => u => u.CreatedDate
            };
        }
    }
}