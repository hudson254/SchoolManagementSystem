using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class GradeRepository : IGradeRepository
    {
        private readonly ApplicationDbContext _context;

        public GradeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Grade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Unit)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Enrollment)
                .Where(g => !g.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Grade>> FindAsync(Expression<Func<Grade, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Enrollment)
                .Where(g => !g.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Grade> AddAsync(Grade entity, CancellationToken cancellationToken = default)
        {
            await _context.Grades.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Grade>> AddRangeAsync(IEnumerable<Grade> entities, CancellationToken cancellationToken = default)
        {
            await _context.Grades.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Grade entity, CancellationToken cancellationToken = default)
        {
            _context.Grades.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Grade entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Grades.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Grade, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Grades.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Grade, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Grades.Where(g => !g.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<Grade?> GetGradeByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Unit)
                .FirstOrDefaultAsync(g => g.EnrollmentId == enrollmentId && !g.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetStudentGradesAsync(Guid studentId, Guid? semesterId, bool? isPublished, CancellationToken cancellationToken = default)
        {
            var query = _context.Grades
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Unit)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Semester)
                .Where(g => g.StudentId == studentId && !g.IsDeleted);

            if (semesterId.HasValue)
                query = query.Where(g => g.Enrollment.SemesterId == semesterId.Value);

            if (isPublished.HasValue)
                query = query.Where(g => g.IsPublished == isPublished.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetUnitGradesAsync(Guid unitId, Guid? semesterId, CancellationToken cancellationToken = default)
        {
            var query = _context.Grades
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Unit)
                .Where(g => g.Enrollment.UnitId == unitId && !g.IsDeleted);

            if (semesterId.HasValue)
                query = query.Where(g => g.Enrollment.SemesterId == semesterId.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetSemesterGradesAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Include(g => g.Student)
                    .ThenInclude(s => s.User)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Unit)
                .Where(g => g.Enrollment.SemesterId == semesterId && !g.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetPublishedGradesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Enrollment)
                .Where(g => g.IsPublished && !g.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountGradesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Where(g => !g.IsDeleted)
                .CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetGradesForSemesterAsync(Guid? semesterId, CancellationToken cancellationToken = default)
        {
            var query = _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Unit)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Semester)
                .Where(g => !g.IsDeleted);

            if (semesterId.HasValue)
                query = query.Where(g => g.Enrollment.SemesterId == semesterId.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetAllGradesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Unit)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Semester)
                .Where(g => !g.IsDeleted)
                .ToListAsync(cancellationToken);
        }
    }
}