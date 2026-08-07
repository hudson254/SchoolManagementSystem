using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ICourseOfferingRepository : IRepository<CourseOffering>
    {
        Task<IEnumerable<CourseOffering>> GetByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOffering>> GetByAcademicYearAsync(Guid academicYearId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOffering>> GetBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOffering>> GetActiveOfferingsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOffering>> GetUpcomingOfferingsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<CourseOffering>> GetCompletedOfferingsAsync(CancellationToken cancellationToken = default);
        Task<CourseOffering> GetByCodeAsync(string offeringCode, CancellationToken cancellationToken = default);
        Task<CourseOffering> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string offeringCode, Guid tenantId, CancellationToken cancellationToken = default);
        Task<string> GenerateOfferingCodeAsync(string courseCode, int academicYear, int semesterNumber, int sequence, CancellationToken cancellationToken = default);
        Task<int> GetNextSequenceForCourseAsync(Guid courseId, int academicYear, int semesterNumber, CancellationToken cancellationToken = default);
    }
}
