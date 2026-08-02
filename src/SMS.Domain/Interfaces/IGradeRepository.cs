using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IGradeRepository : IRepository<Grade>
    {
        Task<IEnumerable<Grade>> GetGradesByStudentAsync(Guid studentId);
        Task<IEnumerable<Grade>> GetGradesByUnitAsync(Guid unitId);
        Task<double> GetStudentGPAAsync(Guid studentId);
        Task<IEnumerable<Grade>> GetStudentGradesAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Grade>> GetGradesForSemesterAsync(Guid semesterId, CancellationToken cancellationToken = default);
        Task<int> CountGradesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Grade>> GetAllGradesAsync(CancellationToken cancellationToken = default);
    }
}
