using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Repository for semester records. Semesters scope accommodation assignments
    /// and academic activities.
    /// </summary>
    public interface ISemesterRepository
    {
        Task<IEnumerable<Semester>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Semester?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the semester that accommodation assignments should default to:
        /// the explicitly current semester if flagged, otherwise the first active
        /// semester, otherwise the most recently started semester. Returns null
        /// when no semester exists in the tenant.
        /// </summary>
        Task<Semester?> GetCurrentOrDefaultAsync(CancellationToken cancellationToken = default);
    }
}