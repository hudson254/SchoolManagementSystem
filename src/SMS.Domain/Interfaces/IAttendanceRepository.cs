using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IAttendanceRepository : IRepository<Attendance>
    {
        Task<IEnumerable<Attendance>> GetAttendancesByStudentAsync(Guid studentId);
        Task<IEnumerable<Attendance>> GetAttendancesByClassAsync(Guid classId);
        Task<Attendance> GetAttendanceByStudentAndClassAsync(Guid studentId, Guid classId, DateTime date);
        Task<IEnumerable<Attendance>> GetAttendanceByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> CountAttendancesAsync(CancellationToken cancellationToken = default);
    }
}
