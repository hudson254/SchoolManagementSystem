using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IStudentRepository : IRepository<Student>
    {
        Task<Student?> GetStudentWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Student>> GetStudentsByProgrammeAsync(Guid programmeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Student>> GetStudentsBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByNumberAsync(string studentNumber, CancellationToken cancellationToken = default);
        Task<IEnumerable<Student>> GetActiveStudentsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Student>> GetGraduatedStudentsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Student>> GetStudentsWithPendingEnrollmentsAsync(CancellationToken cancellationToken = default);
    }
}