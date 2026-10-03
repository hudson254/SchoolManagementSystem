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
    public class LecturerRepository : BaseRepository<Lecturer>, ILecturerRepository
    {
        public LecturerRepository(ApplicationDbContext context, ILogger<LecturerRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Lecturer>> GetLecturersByDepartmentAsync(Guid departmentId)
        {
            return await _dbSet.Where(l => l.DepartmentId == departmentId && !l.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Lecturer>> GetActiveLecturersAsync()
        {
            return await _dbSet.Where(l => l.IsActive && !l.IsDeleted).ToListAsync();
        }

        public async Task<Lecturer> GetLecturerByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(l => l.Email == email && !l.IsDeleted);
        }

        public async Task<int> CountLecturersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(l => !l.IsDeleted, cancellationToken);
        }

        public async Task<Lecturer> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(l => l.UserId == userId.ToString() && !l.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Guid>> GetTaughtUnitIdsAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            var result = new HashSet<Guid>();

            // 1. The lecturer's own unit allocations. Only ACTIVE rows count:
            // registration creates them as "PendingApproval" and
            // ApproveRegistrationCommand is what flips them to "Active", so a
            // lecturer awaiting approval is entitled to nothing here.
            var allocations = await _context.Set<UnitAllocation>()
                .Where(u => u.LecturerId == lecturerId && u.Status == "Active" && !u.IsDeleted)
                .Select(u => new { u.UnitId, u.CourseOfferingId })
                .ToListAsync(cancellationToken);

            if (allocations.Count == 0)
            {
                return result;
            }

            // 2. An allocation that belongs to a course offering is only a valid
            // teaching entitlement while the lecturer's teaching assignment for
            // THAT offering is itself active. Registration writes the assignment as
            // "PendingConfirmation"; ApproveRegistrationCommand activates it.
            //
            // Filtering on Status (not merely IsActive) is what stops a pending
            // lecturer from being treated as a teacher, and restricting the unit set
            // to this lecturer's own allocations is what stops a shared offering
            // snapshot from leaking another lecturer's units into this lecturer's
            // entitlement.
            var offeringIds = allocations
                .Where(a => a.CourseOfferingId.HasValue && a.CourseOfferingId.Value != Guid.Empty)
                .Select(a => a.CourseOfferingId!.Value)
                .Distinct()
                .ToList();

            var activeOfferingIds = offeringIds.Count == 0
                ? new HashSet<Guid>()
                : new HashSet<Guid>(await _context.Set<CourseOfferingLecturer>()
                    .Where(l => l.LecturerId == lecturerId &&
                                offeringIds.Contains(l.CourseOfferingId) &&
                                l.Status == "Active" && l.IsActive && !l.IsDeleted)
                    .Select(l => l.CourseOfferingId)
                    .Distinct()
                    .ToListAsync(cancellationToken));

            foreach (var allocation in allocations)
            {
                // An allocation with no offering is a direct administrative
                // teaching appointment and stands on its own.
                if (!allocation.CourseOfferingId.HasValue || allocation.CourseOfferingId.Value == Guid.Empty)
                {
                    result.Add(allocation.UnitId);
                    continue;
                }

                if (activeOfferingIds.Contains(allocation.CourseOfferingId.Value))
                {
                    result.Add(allocation.UnitId);
                }
            }

            return result;
        }
    }
}

