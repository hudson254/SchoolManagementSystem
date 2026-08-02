using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface ILecturerRepository : IRepository<Lecturer>
    {
        Task<Lecturer?> GetLecturerWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Lecturer>> GetLecturersByUnitAsync(Guid unitId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Lecturer>> GetLecturersBySemesterAsync(Guid semesterId, CancellationToken cancellationToken = default);
        Task<Lecturer?> GetLecturerByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);
        Task<IEnumerable<Lecturer>> GetVerifiedLecturersAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Lecturer>> GetUnverifiedLecturersAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Lecturer>> GetActiveLecturersAsync(CancellationToken cancellationToken = default);
    }
}