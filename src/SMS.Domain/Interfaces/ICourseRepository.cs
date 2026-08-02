using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ICourseRepository : IRepository<Course>
    {
        Task<IEnumerable<Course>> GetCoursesByDepartmentAsync(Guid departmentId);
        Task<IEnumerable<Course>> GetActiveCoursesAsync();
        Task<Course> GetCourseWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Course> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<IEnumerable<Course>> GetCoursesAsync(int page, int pageSize, string? searchTerm, Guid? departmentId, bool? isActive, string sortBy, bool sortDescending, CancellationToken cancellationToken = default);
        Task<int> CountCoursesAsync(string? searchTerm, Guid? departmentId, bool? isActive, CancellationToken cancellationToken = default);
        Task<bool> HasActiveUnitsAsync(Guid courseId, CancellationToken cancellationToken = default);
    }
}
