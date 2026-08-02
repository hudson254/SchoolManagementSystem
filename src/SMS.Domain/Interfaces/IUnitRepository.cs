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
