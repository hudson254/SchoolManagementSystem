using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class AssignmentRepository : IAssignmentRepository
    {
        private readonly ApplicationDbContext _context;

        public AssignmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                    .ThenInclude(l => l.User)
                .Include(a => a.Semester)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                .Include(a => a.Semester)
                .Where(a => !a.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> FindAsync(Expression<Func<Assignment, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                .Include(a => a.Semester)
                .Where(a => !a.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Assignment> AddAsync(Assignment entity, CancellationToken cancellationToken = default)
        {
            await _context.Assignments.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Assignment>> AddRangeAsync(IEnumerable<Assignment> entities, CancellationToken cancellationToken = default)
        {
            await _context.Assignments.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Assignment entity, CancellationToken cancellationToken = default)
        {
            _context.Assignments.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Assignment entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Assignments.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Assignment, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Assignments.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Assignment, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Assignments.Where(a => !a.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<Assignment?> GetAssignmentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                    .ThenInclude(l => l.User)
                .Include(a => a.Semester)
                .Include(a => a.Submissions)
                    .ThenInclude(s => s.Student)
                        .ThenInclude(st => st.User)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                .Include(a => a.Semester)
                .Where(a => a.UnitId == unitId && !a.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                .Include(a => a.Semester)
                .Where(a => a.LecturerId == lecturerId && !a.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                .Include(a => a.Semester)
                .Where(a => a.SemesterId == semesterId && !a.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            Guid? unitId,
            Guid? lecturerId,
            Guid? semesterId,
            string? status,
            bool? isGraded,
            string sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Assignments
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                    .ThenInclude(l => l.User)
                .Include(a => a.Semester)
                .Where(a => !a.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a =>
                    a.Title.Contains(searchTerm) ||
                    a.Description.Contains(searchTerm));
            }

            if (unitId.HasValue)
                query = query.Where(a => a.UnitId == unitId.Value);

            if (lecturerId.HasValue)
                query = query.Where(a => a.LecturerId == lecturerId.Value);

            if (semesterId.HasValue)
                query = query.Where(a => a.SemesterId == semesterId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            if (isGraded.HasValue)
                query = query.Where(a => a.IsGraded == isGraded.Value);

            query = sortDescending
                ? query.OrderByDescending(GetSortExpression(sortBy))
                : query.OrderBy(GetSortExpression(sortBy));

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountAssignmentsAsync(
            string? searchTerm,
            Guid? unitId,
            Guid? lecturerId,
            Guid? semesterId,
            string? status,
            bool? isGraded,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Assignments.Where(a => !a.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a =>
                    a.Title.Contains(searchTerm) ||
                    a.Description.Contains(searchTerm));
            }

            if (unitId.HasValue)
                query = query.Where(a => a.UnitId == unitId.Value);

            if (lecturerId.HasValue)
                query = query.Where(a => a.LecturerId == lecturerId.Value);

            if (semesterId.HasValue)
                query = query.Where(a => a.SemesterId == semesterId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            if (isGraded.HasValue)
                query = query.Where(a => a.IsGraded == isGraded.Value);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Assignments
                .Where(a => !a.IsDeleted)
                .CountAsync(cancellationToken);
        }

        public async Task<AssignmentSubmission?> GetSubmissionAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                    .ThenInclude(st => st.User)
                .FirstOrDefaultAsync(s =>
                    s.AssignmentId == assignmentId &&
                    s.StudentId == studentId &&
                    !s.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<AssignmentSubmission>> GetSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            return await _context.AssignmentSubmissions
                .Include(s => s.Student)
                    .ThenInclude(st => st.User)
                .Where(s => s.AssignmentId == assignmentId && !s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<AssignmentSubmission?> GetSubmissionWithDetailsAsync(Guid submissionId, CancellationToken cancellationToken = default)
        {
            return await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                    .ThenInclude(st => st.User)
                .FirstOrDefaultAsync(s => s.Id == submissionId && !s.IsDeleted, cancellationToken);
        }

        public async Task<AssignmentSubmission> AddSubmissionAsync(AssignmentSubmission submission, CancellationToken cancellationToken = default)
        {
            await _context.AssignmentSubmissions.AddAsync(submission, cancellationToken);
            return submission;
        }

        public Task UpdateSubmissionAsync(AssignmentSubmission submission, CancellationToken cancellationToken = default)
        {
            _context.AssignmentSubmissions.Update(submission);
            return Task.CompletedTask;
        }

        public async Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            return await _context.AssignmentSubmissions
                .AnyAsync(s => s.AssignmentId == assignmentId && !s.IsDeleted, cancellationToken);
        }

        public async Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _context.StudentEnrollments
                .AnyAsync(e =>
                    e.StudentId == studentId &&
                    e.UnitId == unitId &&
                    e.Status != "Dropped" &&
                    !e.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetUpcomingDeadlinesAsync(int days, CancellationToken cancellationToken = default)
        {
            var deadline = DateTime.UtcNow.AddDays(days);
            return await _context.Assignments
                .Where(a =>
                    a.DueDate >= DateTime.UtcNow &&
                    a.DueDate <= deadline &&
                    a.Status == "Published" &&
                    !a.IsDeleted)
                .Include(a => a.Unit)
                .Include(a => a.Lecturer)
                .OrderBy(a => a.DueDate)
                .ToListAsync(cancellationToken);
        }

        private static Expression<Func<Assignment, object>> GetSortExpression(string sortBy)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "title" => a => a.Title,
                "duedate" => a => a.DueDate,
                "createddate" => a => a.CreatedDate,
                _ => a => a.CreatedDate
            };
        }
    }
}