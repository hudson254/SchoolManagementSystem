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
    public class CourseOfferingRepository : BaseRepository<CourseOffering>, ICourseOfferingRepository
    {
        public CourseOfferingRepository(ApplicationDbContext context, ILogger<CourseOfferingRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<CourseOffering>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.CourseId == courseId && !o.IsDeleted)
                .OrderByDescending(o => o.AcademicYearId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOffering>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.AcademicYearId == academicYearId && !o.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOffering>> GetBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.SemesterId == semesterId && !o.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOffering>> GetActiveOfferingsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.Status == CourseOfferingStatus.Active && !o.IsDeleted)
                .Include(o => o.Course)
                .Include(o => o.AcademicYear)
                .Include(o => o.Semester)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOffering>> GetUpcomingOfferingsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.Status == CourseOfferingStatus.Draft && !o.IsDeleted)
                .Include(o => o.Course)
                .Include(o => o.AcademicYear)
                .Include(o => o.Semester)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOffering>> GetCompletedOfferingsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.Status == CourseOfferingStatus.Completed && !o.IsDeleted)
                .Include(o => o.Course)
                .Include(o => o.AcademicYear)
                .Include(o => o.Semester)
                .ToListAsync(cancellationToken);
        }

        public async Task<CourseOffering> GetByCodeAsync(string offeringCode, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(o => o.OfferingCode == offeringCode && !o.IsDeleted, cancellationToken);
        }

        public async Task<CourseOffering> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.Course)
                .Include(o => o.AcademicYear)
                .Include(o => o.Semester)
                .Include(o => o.Units)
                .Include(o => o.Enrollments)
                .Include(o => o.Lecturers)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsByCodeAsync(string offeringCode, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(o => o.OfferingCode == offeringCode && o.TenantId == tenantId && !o.IsDeleted, cancellationToken);
        }

        public async Task<string> GenerateOfferingCodeAsync(
            string courseCode,
            int academicYear,
            int semesterNumber,
            int sequence,
            CancellationToken cancellationToken = default)
        {
            return $"{courseCode}-{academicYear}-S{semesterNumber}-{sequence:D3}";
        }

        public async Task<int> GetNextSequenceForCourseAsync(
            Guid courseId,
            int academicYear,
            int semesterNumber,
            CancellationToken cancellationToken = default)
        {
            var offerings = await _dbSet
                .Where(o => o.CourseId == courseId && !o.IsDeleted)
                .ToListAsync(cancellationToken);

            return offerings.Count + 1;
        }
    }
}
