using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class TimetableRepository : ITimetableRepository
    {
        private readonly ApplicationDbContext _context;

        public TimetableRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Timetable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => !t.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> FindAsync(Expression<Func<Timetable, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => !t.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Timetable> AddAsync(Timetable entity, CancellationToken cancellationToken = default)
        {
            await _context.Timetables.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Timetable>> AddRangeAsync(IEnumerable<Timetable> entities, CancellationToken cancellationToken = default)
        {
            await _context.Timetables.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Timetable entity, CancellationToken cancellationToken = default)
        {
            _context.Timetables.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Timetable entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Timetables.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Timetable, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Timetables.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Timetable, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Timetables.Where(t => !t.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByClassAsync(
            Guid classId,
            string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => t.ClassId == classId && !t.IsDeleted);

            if (!string.IsNullOrEmpty(dayOfWeek))
                query = query.Where(t => t.DayOfWeek == dayOfWeek);

            return await query
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByLecturerAsync(
            Guid lecturerId,
            Guid semesterId,
            string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => t.Class.LecturerId == lecturerId && t.SemesterId == semesterId && !t.IsDeleted);

            if (!string.IsNullOrEmpty(dayOfWeek))
                query = query.Where(t => t.DayOfWeek == dayOfWeek);

            return await query
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByStudentAsync(
            Guid studentId,
            Guid semesterId,
            string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            // Get all classes the student is enrolled in
            var enrollments = await _context.StudentEnrollments
                .Where(e => e.StudentId == studentId && e.SemesterId == semesterId && e.Status != "Dropped" && !e.IsDeleted)
                .Select(e => e.UnitId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!enrollments.Any())
                return new List<Timetable>();

            var query = _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => t.SemesterId == semesterId && !t.IsDeleted);

            // Get classes for units the student is enrolled in
            query = query.Where(t => enrollments.Contains(t.Class.UnitId));

            if (!string.IsNullOrEmpty(dayOfWeek))
                query = query.Where(t => t.DayOfWeek == dayOfWeek);

            return await query
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableBySemesterAsync(
            Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => t.SemesterId == semesterId && !t.IsDeleted)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByRoomAsync(
            string venue,
            Guid semesterId,
            string? dayOfWeek = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => t.Venue == venue && t.SemesterId == semesterId && !t.IsDeleted);

            if (!string.IsNullOrEmpty(dayOfWeek))
                query = query.Where(t => t.DayOfWeek == dayOfWeek);

            return await query
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> CheckForConflictsAsync(
            Guid classId,
            string dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            Guid semesterId,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Timetables
                .Where(t => t.SemesterId == semesterId && !t.IsDeleted);

            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            // Check for overlapping time slots
            var conflicts = await query
                .Where(t => t.DayOfWeek == dayOfWeek &&
                    t.ClassId != classId &&
                    ((startTime >= t.StartTime && startTime < t.EndTime) ||
                     (endTime > t.StartTime && endTime <= t.EndTime) ||
                     (startTime <= t.StartTime && endTime >= t.EndTime)))
                .ToListAsync(cancellationToken);

            return conflicts.Any();
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByUnitAsync(
            Guid unitId,
            Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => t.Class.UnitId == unitId && t.SemesterId == semesterId && !t.IsDeleted)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Timetable>> GetTimetableByDateRangeAsync(
            Guid semesterId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            // This assumes a date range filter based on the semester dates
            // and the day of week matching
            var timetables = await _context.Timetables
                .Include(t => t.Class)
                    .ThenInclude(c => c.Unit)
                .Include(t => t.Class)
                    .ThenInclude(c => c.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(t => t.Semester)
                .Where(t => t.SemesterId == semesterId && !t.IsDeleted)
                .ToListAsync(cancellationToken);

            // Filter by date range using the day of week
            var daysInRange = new List<DateTime>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                daysInRange.Add(date);
            }

            var filteredTimetables = timetables
                .Where(t => daysInRange.Any(d => d.DayOfWeek == t.DayOfWeek))
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToList();

            return filteredTimetables;
        }

        public async Task<IEnumerable<string>> GetAvailableVenuesAsync(
            string dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            var occupiedVenues = await _context.Timetables
                .Where(t => t.SemesterId == semesterId &&
                    t.DayOfWeek == dayOfWeek &&
                    !t.IsDeleted &&
                    ((startTime >= t.StartTime && startTime < t.EndTime) ||
                     (endTime > t.StartTime && endTime <= t.EndTime) ||
                     (startTime <= t.StartTime && endTime >= t.EndTime)))
                .Select(t => t.Venue)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Get all venues from the system
            var allVenues = await _context.Classrooms
                .Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => c.Name)
                .ToListAsync(cancellationToken);

            return allVenues.Except(occupiedVenues).ToList();
        }
    }
}