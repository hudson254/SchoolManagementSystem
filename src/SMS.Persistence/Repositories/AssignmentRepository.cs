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
    public class AssignmentRepository : BaseRepository<Assignment>, IAssignmentRepository
    {
        public AssignmentRepository(ApplicationDbContext context, ILogger<AssignmentRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByUnitAsync(Guid unitId)
        {
            return await _dbSet.Where(a => a.UnitId == unitId && !a.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsByStudentAsync(Guid studentId)
        {
            var enrolledUnitIds = await _context.Set<Enrollment>()
                .Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.Course)
                .ThenInclude(c => c.Units)
                .SelectMany(e => e.Course.Units.Select(u => u.Id))
                .Distinct()
                .ToListAsync();

            return await _dbSet.Where(a => enrolledUnitIds.Contains(a.UnitId) && !a.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Assignment>> GetUpcomingAssignmentsAsync(int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(days);
            return await _dbSet.Where(a => a.DueDate <= cutoff && a.DueDate >= DateTime.UtcNow && !a.IsDeleted).ToListAsync();
        }

        public async Task<Assignment> GetAssignmentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Unit)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Assignment>> GetAssignmentsAsync(
            int page, int pageSize, Guid? unitId, Guid? studentId, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(a => !a.IsDeleted).AsQueryable();

            if (unitId.HasValue)
                query = query.Where(a => a.UnitId == unitId.Value);
            if (studentId.HasValue)
            {
                var enrolledUnitIds = await _context.Set<Enrollment>()
                    .Where(e => e.StudentId == studentId.Value && !e.IsDeleted)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Units)
                    .SelectMany(e => e.Course.Units.Select(u => u.Id))
                    .Distinct()
                    .ToListAsync(cancellationToken);
                query = query.Where(a => enrolledUnitIds.Contains(a.UnitId));
            }

            return await query.OrderByDescending(a => a.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountAssignmentsAsync(Guid? unitId, Guid? studentId, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(a => !a.IsDeleted).AsQueryable();
            if (unitId.HasValue)
                query = query.Where(a => a.UnitId == unitId.Value);
            if (studentId.HasValue)
            {
                var enrolledUnitIds = await _context.Set<Enrollment>()
                    .Where(e => e.StudentId == studentId.Value && !e.IsDeleted)
                    .Include(e => e.Course)
                    .ThenInclude(c => c.Units)
                    .SelectMany(e => e.Course.Units.Select(u => u.Id))
                    .Distinct()
                    .ToListAsync(cancellationToken);
                query = query.Where(a => enrolledUnitIds.Contains(a.UnitId));
            }
            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<AssignmentSubmission>> GetSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AssignmentSubmission>()
                .Where(s => s.AssignmentId == assignmentId)
                .ToListAsync(cancellationToken);
        }

        public async Task<AssignmentSubmission> GetSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AssignmentSubmission>()
                .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);
        }

        public async Task<AssignmentSubmission> GetSubmissionWithDetailsAsync(Guid submissionId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AssignmentSubmission>()
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);
        }

        public async Task<bool> HasSubmissionsAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<AssignmentSubmission>()
                .AnyAsync(s => s.AssignmentId == assignmentId, cancellationToken);
        }

        public async Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Enrollment>()
                .Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.Course)
                .ThenInclude(c => c.Units)
                .AnyAsync(e => e.Course.Units.Any(u => u.Id == unitId), cancellationToken);
        }

        public async Task<AssignmentSubmission> AddSubmissionAsync(AssignmentSubmission submission, CancellationToken cancellationToken = default)
        {
            await _context.Set<AssignmentSubmission>().AddAsync(submission, cancellationToken);
            return submission;
        }

        public Task UpdateSubmission(AssignmentSubmission submission, CancellationToken cancellationToken = default)
        {
            _context.Set<AssignmentSubmission>().Update(submission);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Assignment>> GetUpcomingDeadlinesAsync(Guid studentId, int days, CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(days);
            var enrolledUnitIds = await _context.Set<Enrollment>()
                .Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.Course)
                .ThenInclude(c => c.Units)
                .SelectMany(e => e.Course.Units.Select(u => u.Id))
                .Distinct()
                .ToListAsync(cancellationToken);

            return await _dbSet.Where(a => enrolledUnitIds.Contains(a.UnitId)
                && a.DueDate <= cutoff && a.DueDate >= DateTime.UtcNow && !a.IsDeleted)
                .OrderBy(a => a.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(a => !a.IsDeleted, cancellationToken);
        }
    }
}

