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
    public class EnrollmentRepository : BaseRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(ApplicationDbContext context, ILogger<EnrollmentRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByStudentAsync(Guid studentId)
        {
            return await _dbSet.Where(e => e.StudentId == studentId && !e.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId)
        {
            return await _dbSet.Where(e => e.CourseId == courseId && !e.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByUnitAsync(Guid unitId)
        {
            // Enrollment doesn't have UnitId, so find by course of the unit
            return await _dbSet.Where(e => !e.IsDeleted)
                .Include(e => e.Course)
                .ThenInclude(c => c.Units)
                .Where(e => e.Course.Units.Any(u => u.Id == unitId))
                .ToListAsync();
        }

        public async Task<IEnumerable<Enrollment>> GetActiveEnrollmentsAsync()
        {
            return await _dbSet.Where(e => e.Status == "Active" && !e.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsBySemesterAsync(string semester)
        {
            return await _dbSet.Where(e => e.Semester.Name == semester && !e.IsDeleted)
                .Include(e => e.Semester)
                .ToListAsync();
        }

        public async Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid unitId)
        {
            return await _dbSet.Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.Course)
                .ThenInclude(c => c.Units)
                .AnyAsync(e => e.Course.Units.Any(u => u.Id == unitId));
        }

        public async Task<int> GetEnrollmentCountByCourseAsync(Guid courseId)
        {
            return await _dbSet.CountAsync(e => e.CourseId == courseId && !e.IsDeleted);
        }

        public async Task<int> GetEnrollmentCountByUnitAsync(Guid unitId)
        {
            return await _dbSet.Where(e => !e.IsDeleted)
                .Include(e => e.Course)
                .ThenInclude(c => c.Units)
                .CountAsync(e => e.Course.Units.Any(u => u.Id == unitId));
        }

        public async Task<Enrollment> GetEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId && !e.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(e => !e.IsDeleted).Include(e => e.Student).Include(e => e.Course).ToListAsync(cancellationToken);
        }

        public async Task<int> CountEnrollmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(e => !e.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.Course)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<object>> GetProgrammeEnrollmentCountsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(e => !e.IsDeleted)
                .GroupBy(e => e.Student.Programme.Name)
                .Select(g => new { Programme = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(e => e.EnrollmentDate.Year == year && !e.IsDeleted)
                .Include(e => e.Student)
                .Include(e => e.Course)
                .ToListAsync(cancellationToken);
        }
    }
}

