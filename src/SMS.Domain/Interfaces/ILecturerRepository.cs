using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface ILecturerRepository : IRepository<Lecturer>
    {
        Task<IEnumerable<Lecturer>> GetLecturersByDepartmentAsync(Guid departmentId);
        Task<IEnumerable<Lecturer>> GetActiveLecturersAsync();
        Task<Lecturer> GetLecturerByEmailAsync(string email);
        Task<int> CountLecturersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds the lecturer profile linked to the given Identity user id
        /// (e.g. the currently signed-in lecturer). Returns null when no
        /// lecturer profile exists for that user.
        /// </summary>
        Task<Lecturer> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the distinct unit ids this lecturer is authorized to teach,
        /// derived from unit allocations and course-offering lecturer assignments.
        /// </summary>
        Task<IEnumerable<Guid>> GetTaughtUnitIdsAsync(Guid lecturerId, CancellationToken cancellationToken = default);
    }
}
