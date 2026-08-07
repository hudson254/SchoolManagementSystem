using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class CourseOfferingEnrollmentRepository : BaseRepository<CourseOfferingEnrollment>, ICourseOfferingEnrollmentRepository
    {
        public CourseOfferingEnrollmentRepository(ApplicationDbContext context, ILogger<CourseOfferingEnrollmentRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<CourseOfferingEnrollment>> GetByOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => e.CourseOfferingId == courseOfferingId && !e.IsDeleted)
                .Include(e => e.Student)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingEnrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<CourseOfferingEnrollment> GetByOfferingAndStudentAsync(Guid courseOfferingId, Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(e => e.CourseOfferingId == courseOfferingId && e.StudentId == studentId && !e.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingEnrollment>> GetPendingConfirmationsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => e.StudentId == studentId && e.ConfirmationStatus == ConfirmationStatus.Pending && !e.IsDeleted)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingEnrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => e.StudentId == studentId && e.Status == "Active" && !e.IsDeleted)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingEnrollment>> GetHistoryByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => e.StudentId == studentId && (e.Status == "Completed" || e.Status == "Dropped") && !e.IsDeleted)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetAttemptCountAsync(Guid courseOfferingId, Guid studentId, CancellationToken cancellationToken = default)
        {
            // Get the course ID for the given offering, then count all enrollments
            // for that student across all offerings of the same course.
            var courseId = await _context.CourseOfferings
                .Where(o => o.Id == courseOfferingId)
                .Select(o => o.CourseId)
                .FirstOrDefaultAsync(cancellationToken);

            var offeringIds = await _context.CourseOfferings
                .Where(o => o.CourseId == courseId)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            return await _dbSet
                .CountAsync(e => offeringIds.Contains(e.CourseOfferingId) && e.StudentId == studentId && !e.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsByOfferingAndStudentAsync(Guid courseOfferingId, Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(e => e.CourseOfferingId == courseOfferingId && e.StudentId == studentId && !e.IsDeleted, cancellationToken);
        }
    }
}
