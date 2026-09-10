using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Repository for academic Class records. Classes scope a unit to a lecturer
    /// within a semester and carry the recurring schedule (day + times). The
    /// Classes table exists in the schema but was not previously exposed through
    /// the API; this repository enables coordinator class management using the
    /// existing domain model.
    /// </summary>
    public interface IClassRepository : IRepository<Class>
    {
        /// <summary>
        /// Returns classes with their Unit, Lecturer, and Semester navigation
        /// properties populated so the API can render names without N+1 queries.
        /// </summary>
        Task<IEnumerable<Class>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    }
}