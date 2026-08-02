using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IDepartmentRepository : IRepository<Department>
    {
        Task<IEnumerable<Department>> GetActiveDepartmentsAsync();
        Task<Department> GetDepartmentByCodeAsync(string code);
        Task<IEnumerable<Department>> GetDepartmentsWithCoursesAsync();
        Task<bool> IsDepartmentCodeUniqueAsync(string code, Guid? excludeId = null);
        Task<Department> GetDepartmentWithCoursesAsync(Guid departmentId);
        Task<IEnumerable<Department>> SearchDepartmentsAsync(string searchTerm);
        Task<int> CountDepartmentsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Department>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default);
    }
}
