using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IUnitAllocationRepository : IRepository<UnitAllocation>
    {
        Task<IEnumerable<UnitAllocation>> GetByLecturerAsync(Guid lecturerId);
        Task<IEnumerable<UnitAllocation>> GetByUnitAsync(Guid unitId);
        Task<IEnumerable<UnitAllocation>> GetBySemesterAsync(Guid semesterId);
        Task<IEnumerable<UnitAllocation>> GetByLecturerAndSemesterAsync(Guid lecturerId, Guid semesterId);
        Task<bool> IsLecturerAllocatedAsync(Guid lecturerId, Guid unitId, Guid semesterId);
        Task<int> GetAllocationCountByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
    }
}
