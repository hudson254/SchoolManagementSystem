using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IStudentRepository : IRepository<Student>
    {
        Task<Student> GetStudentByEmailAsync(string email);
        Task<Student> GetStudentByStudentNumberAsync(string studentNumber);
        Task<IEnumerable<Student>> GetStudentsByCourseAsync(Guid courseId);
        Task<IEnumerable<Student>> GetStudentsByDepartmentAsync(Guid departmentId);
        Task<IEnumerable<Student>> GetActiveStudentsAsync();
        Task<IEnumerable<Student>> GetGraduatingStudentsAsync();
        Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm);
        Task<bool> IsStudentNumberUniqueAsync(string studentNumber, Guid? excludeId = null);
        Task<IEnumerable<Student>> GetStudentsWithEnrollmentsAsync();
        Task<Student> GetStudentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<int> CountStudentsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Student>> GetStudentsByProgrammeAsync(Guid programmeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Student>> GetStudentsBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the distinct unit ids the student is enrolled in, derived from
        /// legacy enrollments (course → units), student-enrollment rows, and active
        /// course-offering enrollments (offering → units).
        /// </summary>
        Task<IEnumerable<Guid>> GetEnrolledUnitIdsAsync(Guid studentId, CancellationToken cancellationToken = default);
    }
}
