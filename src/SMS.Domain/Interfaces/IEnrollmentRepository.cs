using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IEnrollmentRepository : IRepository<Enrollment>
    {
        Task<IEnumerable<Enrollment>> GetEnrollmentsByStudentAsync(Guid studentId);
        Task<IEnumerable<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId);
        Task<IEnumerable<Enrollment>> GetEnrollmentsByUnitAsync(Guid unitId);
        Task<IEnumerable<Enrollment>> GetActiveEnrollmentsAsync();
        Task<IEnumerable<Enrollment>> GetEnrollmentsBySemesterAsync(string semester);
        Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid unitId);
        Task<int> GetEnrollmentCountByCourseAsync(Guid courseId);
        Task<int> GetEnrollmentCountByUnitAsync(Guid unitId);
        Task<Enrollment> GetEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Enrollment>> GetEnrollmentsAsync(CancellationToken cancellationToken = default);
        Task<int> CountEnrollmentsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<object>> GetProgrammeEnrollmentCountsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Enrollment>> GetEnrollmentsByYearAsync(int year, CancellationToken cancellationToken = default);
    }
}
