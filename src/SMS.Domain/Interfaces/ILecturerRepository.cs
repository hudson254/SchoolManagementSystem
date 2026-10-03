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
        /// Returns the distinct unit ids this lecturer is currently AUTHORIZED to
        /// teach.
        /// <para>
        /// Derived from persisted data only, and deliberately narrow so a lecturer
        /// can never gain a unit that was not explicitly allocated to them:
        /// </para>
        /// <list type="number">
        /// <item>the lecturer's own ACTIVE <c>UnitAllocation</c> rows - this is the
        /// record of the units the lecturer actually selected; a
        /// <c>PendingApproval</c> allocation therefore grants nothing;</item>
        /// <item>when such an allocation is linked to a course offering, the offering
        /// must ALSO carry an ACTIVE (<c>Status == "Active"</c>) teaching assignment
        /// for this lecturer. Registration writes that assignment as
        /// <c>PendingConfirmation</c> and only approval activates it, so a
        /// still-pending lecturer is not entitled to anything.</item>
        /// </list>
        /// <para>
        /// The previous implementation expanded every <c>CourseOfferingLecturer</c> row
        /// (filtering only <c>IsActive</c>, never <c>Status</c>) to EVERY unit of that
        /// offering. Because the offering snapshot is shared by all lecturers of the
        /// course, one lecturer's pending registration granted every lecturer — and
        /// every pending lecturer — the whole course.
        /// </para>
        /// </summary>
        Task<IEnumerable<Guid>> GetTaughtUnitIdsAsync(Guid lecturerId, CancellationToken cancellationToken = default);
    }
}
