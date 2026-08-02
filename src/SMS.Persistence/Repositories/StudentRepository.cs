using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Programme)
                .Include(s => s.CurrentSemester)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Programme)
                .Where(s => !s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Student>> FindAsync(Expression<Func<Student, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Programme)
                .Where(s => !s.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Student> AddAsync(Student entity, CancellationToken cancellationToken = default)
        {
            await _context.Students.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Student>> AddRangeAsync(IEnumerable<Student> entities, CancellationToken cancellationToken = default)
        {
            await _context.Students.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Student entity, CancellationToken cancellationToken = default)
        {
            _context.Students.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Student entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Students.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Student, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Students.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Student, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Students.Where(s => !s.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<Student?> GetStudentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Programme)
                .Include(s => s.CurrentSemester)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Unit)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Semester)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Enrollment)
                        .ThenInclude(e => e.Unit)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Enrollment)
                        .ThenInclude(e => e.Semester)
                .Include(s => s.AccommodationAssignment)
                    .ThenInclude(a => a.Room)
                        .ThenInclude(r => r.Block)
                            .ThenInclude(b => b.Building)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetStudentsByProgrammeAsync(Guid programmeId, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Where(s => s.ProgrammeId == programmeId && !s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetStudentsBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Where(s => s.CurrentSemesterId == semesterId && !s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<Student?> GetStudentByNumberAsync(string studentNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber && !s.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetActiveStudentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Where(s => s.IsEnrolled && s.AcademicStatus == "Active" && !s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetGraduatedStudentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Where(s => s.AcademicStatus == "Graduated" && !s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetStudentsWithPendingEnrollmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(s => s.User)
                .Where(s => s.Enrollments.Any(e => e.Status == "Enrolled" || e.Status == "InProgress") && !s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetStudentsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? academicStatus,
            Guid? programmeId,
            bool? isEnrolled,
            string sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Programme)
                .Where(s => !s.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    s.StudentNumber.Contains(searchTerm) ||
                    s.User.FirstName.Contains(searchTerm) ||
                    s.User.LastName.Contains(searchTerm) ||
                    s.User.Email.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(academicStatus))
                query = query.Where(s => s.AcademicStatus == academicStatus);

            if (programmeId.HasValue)
                query = query.Where(s => s.ProgrammeId == programmeId.Value);

            if (isEnrolled.HasValue)
                query = query.Where(s => s.IsEnrolled == isEnrolled.Value);

            query = sortDescending
                ? query.OrderByDescending(GetSortExpression(sortBy))
                : query.OrderBy(GetSortExpression(sortBy));

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountStudentsAsync(
            string? searchTerm,
            string? academicStatus,
            Guid? programmeId,
            bool? isEnrolled,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Students.Where(s => !s.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    s.StudentNumber.Contains(searchTerm) ||
                    s.User.FirstName.Contains(searchTerm) ||
                    s.User.LastName.Contains(searchTerm) ||
                    s.User.Email.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(academicStatus))
                query = query.Where(s => s.AcademicStatus == academicStatus);

            if (programmeId.HasValue)
                query = query.Where(s => s.ProgrammeId == programmeId.Value);

            if (isEnrolled.HasValue)
                query = query.Where(s => s.IsEnrolled == isEnrolled.Value);

            return await query.CountAsync(cancellationToken);
        }

        private static Expression<Func<Student, object>> GetSortExpression(string sortBy)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "name" => s => s.User.FirstName,
                "studentnumber" => s => s.StudentNumber,
                "enrollmentdate" => s => s.EnrollmentDate,
                "createddate" => s => s.CreatedDate,
                _ => s => s.CreatedDate
            };
        }
    }
}