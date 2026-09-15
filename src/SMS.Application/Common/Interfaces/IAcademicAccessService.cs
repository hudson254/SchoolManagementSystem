using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Centralized academic access checks used by assignment-document and study-material
    /// features. Server-side authorization must be based on real persisted relationships
    /// (lecturer teaches unit, student enrolled in unit, admin/coordinator override),
    /// never on a frontend-supplied flag or role.
    /// </summary>
    public interface IAcademicAccessService
    {
        /// <summary>Resolves the currently authenticated user's lecturer profile.</summary>
        Task<Lecturer?> GetCurrentLecturerAsync(CancellationToken cancellationToken = default);

        /// <summary>Resolves the currently authenticated user's student profile.</summary>
        Task<Student?> GetCurrentStudentAsync(CancellationToken cancellationToken = default);

        bool IsAdminOrCoordinator();

        bool IsLecturerRole();

        bool IsStudentRole();

        /// <summary>
        /// Whether the given lecturer is authorized to teach the given unit
        /// (via unit allocations or course-offering lecturer assignments).
        /// </summary>
        Task<bool> LecturerTeachesUnitAsync(Guid lecturerId, Guid unitId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether the current lecturer is authorized to teach the given unit
        /// (via unit allocations or course-offering lecturer assignments).
        /// </summary>
        Task<bool> LecturerTeachesUnitAsync(Guid unitId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether the current student is enrolled in the given unit
        /// (legacy enrollments, student-enrollment rows, or course-offering units).
        /// </summary>
        Task<bool> StudentEnrolledInUnitAsync(Guid unitId, CancellationToken cancellationToken = default);
    }
}