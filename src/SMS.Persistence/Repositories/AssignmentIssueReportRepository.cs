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
    public class AssignmentIssueReportRepository : BaseRepository<AssignmentIssueReport>, IAssignmentIssueReportRepository
    {
        public AssignmentIssueReportRepository(ApplicationDbContext context, ILogger<AssignmentIssueReportRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<AssignmentIssueReport>> GetPendingAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.Status == AssignmentIssueStatus.Pending && !r.IsDeleted)
                .Include(r => r.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(r => r.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(r => r.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AssignmentIssueReport>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.Status.ToString() == status && !r.IsDeleted)
                .Include(r => r.CourseOffering)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AssignmentIssueReport>> GetByReporterAsync(Guid reporterUserId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.ReporterUserId == reporterUserId && !r.IsDeleted)
                .Include(r => r.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Include(r => r.CourseOffering)
                    .ThenInclude(o => o.Semester)
                .Include(r => r.CourseOffering)
                    .ThenInclude(o => o.AcademicYear)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AssignmentIssueReport>> GetByOfferingAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.CourseOfferingId == courseOfferingId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AssignmentIssueReport>> GetPendingByTypeAsync(string assignmentType, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.AssignmentType == assignmentType && r.Status == AssignmentIssueStatus.Pending && !r.IsDeleted)
                .Include(r => r.CourseOffering)
                    .ThenInclude(o => o.Course)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(cancellationToken);
        }
    }
}
