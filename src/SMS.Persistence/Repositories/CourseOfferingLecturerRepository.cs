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
    public class CourseOfferingLecturerRepository : BaseRepository<CourseOfferingLecturer>, ICourseOfferingLecturerRepository
    {
        public CourseOfferingLecturerRepository(ApplicationDbContext context, ILogger<CourseOfferingLecturerRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<CourseOfferingLecturer>> GetByOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.CourseOfferingId == courseOfferingId && !l.IsDeleted)
                .Include(l => l.Lecturer)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingLecturer>> GetByLecturerIdAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.LecturerId == lecturerId && !l.IsDeleted)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .OrderByDescending(l => l.AssignmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<CourseOfferingLecturer> GetByOfferingAndLecturerAsync(Guid courseOfferingId, Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(l => l.CourseOfferingId == courseOfferingId && l.LecturerId == lecturerId && !l.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingLecturer>> GetPendingConfirmationsByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.LecturerId == lecturerId && l.ConfirmationStatus == ConfirmationStatus.Pending && !l.IsDeleted)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingLecturer>> GetActiveByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.LecturerId == lecturerId && l.Status == "Active" && !l.IsDeleted)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseOfferingLecturer>> GetHistoryByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.LecturerId == lecturerId && (l.Status == "Completed" || l.Status == "Cancelled") && !l.IsDeleted)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(l => l.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .OrderByDescending(l => l.AssignmentDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByOfferingAndLecturerAsync(Guid courseOfferingId, Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(l => l.CourseOfferingId == courseOfferingId && l.LecturerId == lecturerId && !l.IsDeleted, cancellationToken);
        }
    }
}
