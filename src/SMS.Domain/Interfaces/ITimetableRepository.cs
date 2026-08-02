using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface ITimetableRepository : IRepository<Timetable>
    {
        Task<IEnumerable<Timetable>> GetTimetableByClassAsync(Guid classId);
        Task<IEnumerable<Timetable>> GetTimetableByLecturerAsync(Guid lecturerId);
        Task<IEnumerable<Timetable>> GetTimetableByRoomAsync(Guid roomId);
        Task<IEnumerable<Timetable>> GetWeeklyTimetableAsync(Guid entityId, DateTime weekStart);
        Task<bool> IsTimeSlotAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<Timetable>> GetTimetableByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<Timetable>> GetTimetableByUnitAsync(Guid unitId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Timetable>> GetTimetableByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<int> CountByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default);
    }
}
