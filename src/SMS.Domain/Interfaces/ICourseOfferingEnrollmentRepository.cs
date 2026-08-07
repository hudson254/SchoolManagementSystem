using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ICourseOfferingEnrollmentRepository : IRepository<CourseOfferingEnrollment>
    {
        Task<IEnumerable<CourseOfferingEnrollment>> GetByOfferingIdAsync(Guid courseOfferingId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingEnrollment>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<CourseOfferingEnrollment> GetByOfferingAndStudentAsync(Guid courseOfferingId, Guid studentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingEnrollment>> GetPendingConfirmationsByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingEnrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOfferingEnrollment>> GetHistoryByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<int> GetAttemptCountAsync(Guid courseOfferingId, Guid studentId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByOfferingAndStudentAsync(Guid courseOfferingId, Guid studentId, CancellationToken cancellationToken = default);
    }
}
