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
    public class CourseOfferingUnitRepository : BaseRepository<CourseOfferingUnit>, ICourseOfferingUnitRepository
    {
        public CourseOfferingUnitRepository(ApplicationDbContext context, ILogger<CourseOfferingUnitRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<CourseOfferingUnit>> GetByOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => u.CourseOfferingId == courseOfferingId && !u.IsDeleted)
                .OrderBy(u => u.Order)
                .ToListAsync(cancellationToken);
        }

        public async Task<CourseOfferingUnit> GetByOfferingAndUnitAsync(Guid courseOfferingId, Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.CourseOfferingId == courseOfferingId && u.UnitId == unitId && !u.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsByOfferingAndUnitAsync(Guid courseOfferingId, Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(u => u.CourseOfferingId == courseOfferingId && u.UnitId == unitId && !u.IsDeleted, cancellationToken);
        }

        public async Task<int> GetMaxOrderAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
        {
            var maxOrder = await _dbSet
                .Where(u => u.CourseOfferingId == courseOfferingId && !u.IsDeleted)
                .MaxAsync(u => (int?)u.Order, cancellationToken);

            return maxOrder ?? 0;
        }

        public async Task<IEnumerable<CourseOfferingUnit>> GetOrderedUnitsAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => u.CourseOfferingId == courseOfferingId && !u.IsDeleted)
                .OrderBy(u => u.Order)
                .ToListAsync(cancellationToken);
        }
    }
}
