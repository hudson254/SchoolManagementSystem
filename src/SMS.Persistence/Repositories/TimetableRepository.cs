using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class TimetableRepository : BaseRepository<Timetable>, ITimetableRepository
    {
        public TimetableRepository(ApplicationDbContext context, ILogger<TimetableRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByClassAsync(Guid classId)
        {
            return await _dbSet.Where(t => t.ClassId == classId && !t.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByLecturerAsync(Guid lecturerId)
        {
            return await _dbSet.Where(t => t.LecturerId == lecturerId && !t.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByRoomAsync(Guid roomId)
        {
            return await _dbSet.Where(t => t.RoomId == roomId && !t.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Timetable>> GetWeeklyTimetableAsync(Guid entityId, DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(7);
            return await _dbSet.Where(t => t.Date >= weekStart.Date && t.Date <= weekEnd.Date && !t.IsDeleted)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync();
        }

        public async Task<bool> IsTimeSlotAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime)
        {
            return !await _dbSet.AnyAsync(t =>
                t.RoomId == roomId &&
                t.StartTime <= startTime.TimeOfDay &&
                t.EndTime >= endTime.TimeOfDay &&
                !t.IsDeleted);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(t => t.Date >= startDate && t.Date <= endDate && !t.IsDeleted)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(t => t.UnitId == unitId && !t.IsDeleted)
                .OrderBy(t => t.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            // Get timetable for units the student is enrolled in
            var enrolledUnitIds = await _context.Set<Enrollment>()
                .Where(e => e.StudentId == studentId && !e.IsDeleted)
                .Include(e => e.Course)
                .ThenInclude(c => c.Units)
                .SelectMany(e => e.Course.Units.Select(u => u.Id))
                .Distinct()
                .ToListAsync(cancellationToken);

            return await _dbSet.Where(t => enrolledUnitIds.Contains(t.UnitId) && !t.IsDeleted)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountByLecturerAsync(Guid lecturerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(t => t.LecturerId == lecturerId && !t.IsDeleted, cancellationToken);
        }
    }
}
