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
    public class StudentRepository : BaseRepository<Student>, IStudentRepository
    {
        public StudentRepository(ApplicationDbContext context, ILogger<StudentRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<Student> GetStudentByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.Email == email && !s.IsDeleted);
        }

        public async Task<Student> GetStudentByStudentNumberAsync(string studentNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber && !s.IsDeleted);
        }

        public async Task<IEnumerable<Student>> GetStudentsByCourseAsync(Guid courseId)
        {
            return await _dbSet.Where(s => s.Enrollments.Any(e => e.CourseId == courseId) && !s.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetStudentsByDepartmentAsync(Guid departmentId)
        {
            return await _dbSet.Where(s => s.Programme.DepartmentId == departmentId && !s.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetActiveStudentsAsync()
        {
            return await _dbSet.Where(s => s.IsActive && !s.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetGraduatingStudentsAsync()
        {
            return await _dbSet.Where(s => s.AcademicStatus == "Graduating" && !s.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm)
        {
            return await _dbSet.Where(s =>
                (s.FirstName + " " + s.LastName).Contains(searchTerm) ||
                s.StudentNumber.Contains(searchTerm) ||
                s.Email.Contains(searchTerm)
            ).ToListAsync();
        }

        public async Task<bool> IsStudentNumberUniqueAsync(string studentNumber, Guid? excludeId = null)
        {
            if (excludeId.HasValue)
                return !await _dbSet.AnyAsync(s => s.StudentNumber == studentNumber && s.Id != excludeId.Value && !s.IsDeleted);
            return !await _dbSet.AnyAsync(s => s.StudentNumber == studentNumber && !s.IsDeleted);
        }

        public async Task<IEnumerable<Student>> GetStudentsWithEnrollmentsAsync()
        {
            return await _dbSet.Include(s => s.Enrollments).Where(s => !s.IsDeleted).ToListAsync();
        }

        public async Task<Student> GetStudentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
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
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Unit)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Semester)
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        }

        public async Task<int> CountStudentsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(s => !s.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetStudentsByProgrammeAsync(Guid programmeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(s => s.ProgrammeId == programmeId && !s.IsDeleted).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetStudentsBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(s => s.CurrentSemesterId == semesterId && !s.IsDeleted).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Guid>> GetEnrolledUnitIdsAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var result = new HashSet<Guid>();

            // 1. Legacy enrollments: Enrollment -> Course -> Units (plus direct UnitId)
            var legacyUnitIds = await _context.Set<Enrollment>()
                .Where(e => e.StudentId == studentId && e.IsActive && !e.IsDeleted)
                .Select(e => e.Course.Units.Select(u => u.Id))
                .SelectMany(ids => ids)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in legacyUnitIds)
            {
                result.Add(id);
            }

            var directUnitIds = await _context.Set<Enrollment>()
                .Where(e => e.StudentId == studentId && e.IsActive && !e.IsDeleted && e.UnitId != null)
                .Select(e => e.UnitId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in directUnitIds)
            {
                result.Add(id);
            }

            // 2. StudentEnrollment rows (per-unit enrollment)
            var studentEnrollmentUnitIds = await _context.Set<StudentEnrollment>()
                .Where(e => e.StudentId == studentId && e.Status == "Enrolled" && !e.IsDeleted)
                .Select(e => e.UnitId)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in studentEnrollmentUnitIds)
            {
                result.Add(id);
            }

            // 3. Active course-offering enrollments -> offering units
            var offeringIds = await _context.Set<CourseOfferingEnrollment>()
                .Where(e => e.StudentId == studentId && e.Status == "Active" && e.IsActive && !e.IsDeleted)
                .Select(e => e.CourseOfferingId)
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

