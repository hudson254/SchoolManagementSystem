using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IUnitRepository : IRepository<Unit>
    {
        Task<IEnumerable<Unit>> GetUnitsByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Unit>> GetUnitsByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Unit>> GetUnitsBySemesterAsync(int semester, CancellationToken cancellationToken = default);
        Task<Unit> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<Unit> GetUnitWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        /// <summary>
        /// Returns the active units matching the supplied ids, with their Course
        /// loaded. Used by the study-material unit selector, which already knows
        /// the exact set of unit ids the caller is entitled to from the persisted
        /// teaching/enrollment relationships and only needs display metadata.
        /// Tenant isolation comes from the global EF Core query filter on
        /// <see cref="Unit"/> (ITenantAwareEntity), so a foreign tenant's unit id
        /// simply resolves to nothing.
        /// </summary>
        Task<IEnumerable<Unit>> GetUnitsByIdsAsync(
            IReadOnlyCollection<Guid> unitIds,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Unit>> GetUnitsAsync(
            int page,
            int pageSize,
            string? searchTerm,
            Guid? courseId,
            int? semester,
            bool? isActive,
            string sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default);
        Task<int> CountUnitsAsync(
            string? searchTerm,
            Guid? courseId,
            int? semester,
            bool? isActive,
            CancellationToken cancellationToken = default);
    }
}
