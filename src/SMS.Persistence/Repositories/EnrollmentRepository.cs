using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudentEnrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Include(e => e.Student)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .Where(e => !e.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> FindAsync(Expression<Func<StudentEnrollment, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Include(e => e.Student)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .Where(e => !e.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<StudentEnrollment> AddAsync(StudentEnrollment entity, CancellationToken cancellationToken = default)
        {
            await _context.StudentEnrollments.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<StudentEnrollment>> AddRangeAsync(IEnumerable<StudentEnrollment> entities, CancellationToken cancellationToken = default)
        {
            await _context.StudentEnrollments.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(StudentEnrollment entity, CancellationToken cancellationToken = default)
        {
            _context.StudentEnrollments.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(StudentEnrollment entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.StudentEnrollments.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<StudentEnrollment, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<StudentEnrollment, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.StudentEnrollments.Where(e => !e.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<StudentEnrollment?> GetEnrollmentAsync(Guid studentId, Guid unitId, Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Include(e => e.Student)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId &&
                    e.UnitId == unitId &&
                    e.SemesterId == semesterId &&
                    !e.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> GetStudentEnrollmentsAsync(Guid studentId, Guid? semesterId, string? status, CancellationToken cancellationToken = default)
        {
            var query = _context.StudentEnrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .Where(e => e.StudentId == studentId && !e.IsDeleted);

            if (semesterId.HasValue)
                query = query.Where(e => e.SemesterId == semesterId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> GetUnitEnrollmentsAsync(Guid unitId, Guid? semesterId, CancellationToken cancellationToken = default)
        {
            var query = _context.StudentEnrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .Where(e => e.UnitId == unitId && !e.IsDeleted);

            if (semesterId.HasValue)
                query = query.Where(e => e.SemesterId == semesterId.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> GetSemesterEnrollmentsAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .Where(e => e.SemesterId == semesterId && !e.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> GetActiveEnrollmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Include(e => e.Student)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .Where(e => e.Status == "Enrolled" || e.Status == "InProgress")
                .Where(e => !e.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountEnrollmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Where(e => !e.IsDeleted)
                .CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<(string ProgrammeName, int Count)>> GetProgrammeEnrollmentCountsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Where(e => !e.IsDeleted && e.Student.Programme != null)
                .GroupBy(e => e.Student.Programme.Name)
                .Select(g => new { ProgrammeName = g.Key, Count = g.Count() })
                .Select(x => (x.ProgrammeName, x.Count))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> GetEnrollmentsByYearAsync(int? academicYearId, CancellationToken cancellationToken = default)
        {
            var query = _context.StudentEnrollments
                .Include(e => e.Student)
                .Include(e => e.Unit)
                .Include(e => e.Semester)
                .ThenInclude(s => s.AcademicYear)
                .Where(e => !e.IsDeleted);

            if (academicYearId.HasValue)
            {
                query = query.Where(e => e.Semester.AcademicYearId == academicYearId.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentEnrollment>> GetEnrollmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .Where(e => !e.IsDeleted)
                .ToListAsync(cancellationToken);
        }
    }
}