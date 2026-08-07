using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ICourseOfferingUnitRepository : IRepository<CourseOfferingUnit>
    {
        Task<IEnumerable<CourseOfferingUnit>> GetByOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
        Task<CourseOfferingUnit> GetByOfferingAndUnitAsync(Guid courseOfferingId, Guid unitId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByOfferingAndUnitAsync(Guid courseOfferingId, Guid unitId, CancellationToken cancellationToken = default);
        Task<int> GetMaxOrderAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingUnit>> GetOrderedUnitsAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
    }
}
