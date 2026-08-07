using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ICourseOfferingLecturerRepository : IRepository<CourseOfferingLecturer>
    {
        Task<IEnumerable<CourseOfferingLecturer>> GetByOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingLecturer>> GetByLecturerIdAsync(Guid lecturerId, CancellationToken cancellationToken = default);
        Task<CourseOfferingLecturer> GetByOfferingAndLecturerAsync(Guid courseOfferingId, Guid lecturerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingLecturer>> GetPendingConfirmationsByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingLecturer>> GetActiveByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingLecturer>> GetHistoryByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByOfferingAndLecturerAsync(Guid courseOfferingId, Guid lecturerId, CancellationToken cancellationToken = default);
    }
}
