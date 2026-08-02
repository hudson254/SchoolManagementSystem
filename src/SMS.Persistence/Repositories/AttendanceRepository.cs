using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Attendance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Attendance>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                .Where(a => !a.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Attendance>> FindAsync(Expression<Func<Attendance, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                .Where(a => !a.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Attendance> AddAsync(Attendance entity, CancellationToken cancellationToken = default)
        {
            await _context.Attendances.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Attendance>> AddRangeAsync(IEnumerable<Attendance> entities, CancellationToken cancellationToken = default)
        {
            await _context.Attendances.AddRangeAsync(entities, cancellationToken);
            return entities;
        }

        public Task UpdateAsync(Attendance entity, CancellationToken cancellationToken = default)
        {
            _context.Attendances.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Attendance entity, CancellationToken cancellationToken = default)
        {
            entity.SoftDelete("SYSTEM");
            _context.Attendances.Update(entity);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Expression<Func<Attendance, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Attendances.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<Attendance, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Attendances.Where(a => !a.IsDeleted);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Attendance>> GetStudentAttendancesAsync(
            Guid studentId,
            Guid? classId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                .Where(a => a.StudentId == studentId && !a.IsDeleted);

            if (classId.HasValue)
                query = query.Where(a => a.ClassId == classId.Value);

            if (fromDate.HasValue)
                query = query.Where(a => a.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Date <= toDate.Value);

            return await query
                .OrderByDescending(a => a.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Attendance>> GetClassAttendancesAsync(
            Guid classId,
            DateTime? date = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                .Where(a => a.ClassId == classId && !a.IsDeleted);

            if (date.HasValue)
                query = query.Where(a => a.Date.Date == date.Value.Date);

            return await query
                .OrderBy(a => a.Student.User.LastName)
                .ThenBy(a => a.Student.User.FirstName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesByDateAsync(
            DateTime date,
            Guid? classId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                .Where(a => a.Date.Date == date.Date && !a.IsDeleted);

            if (classId.HasValue)
                query = query.Where(a => a.ClassId == classId.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesBySemesterAsync(
            Guid semesterId,
            Guid? studentId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                    .ThenInclude(u => u.Course)
                .Where(a => a.Class.SemesterId == semesterId && !a.IsDeleted);

            if (studentId.HasValue)
                query = query.Where(a => a.StudentId == studentId.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(
            Guid studentId,
            Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            var attendances = await _context.Attendances
                .Include(a => a.Class)
                .Where(a => a.StudentId == studentId && a.Class.SemesterId == semesterId && !a.IsDeleted)
                .ToListAsync(cancellationToken);

            var total = attendances.Count;
            var present = attendances.Count(a => a.Status == "Present");
            var absent = attendances.Count(a => a.Status == "Absent");
            var late = attendances.Count(a => a.Status == "Late");
            var excused = attendances.Count(a => a.Status == "Excused");

            var attendanceRate = total > 0 ? (double)present / total * 100 : 0;

            return new AttendanceSummaryDto
            {
                StudentId = studentId,
                SemesterId = semesterId,
                TotalClasses = total,
                Present = present,
                Absent = absent,
                Late = late,
                Excused = excused,
                AttendanceRate = attendanceRate
            };
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesByUnitAsync(
            Guid unitId,
            Guid semesterId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.Class)
                    .ThenInclude(c => c.Unit)
                .Where(a => a.Class.UnitId == unitId && a.Class.SemesterId == semesterId && !a.IsDeleted)
                .OrderBy(a => a.Date)
                .ToListAsync(cancellationToken);
        }

        public async Task<Attendance?> GetAttendanceByStudentAndClassAndDateAsync(
            Guid studentId,
            Guid classId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            return await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.StudentId == studentId &&
                    a.ClassId == classId &&
                    a.Date.Date == date.Date &&
                    !a.IsDeleted, cancellationToken);
        }

        public async Task<ClassAttendanceSummaryDto> GetClassAttendanceSummaryAsync(
            Guid classId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Attendances
                .Where(a => a.ClassId == classId && !a.IsDeleted);

            if (fromDate.HasValue)
                query = query.Where(a => a.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Date <= toDate.Value);

            var attendances = await query.ToListAsync(cancellationToken);

            var totalStudents = attendances.Select(a => a.StudentId).Distinct().Count();
            var totalClasses = attendances.Select(a => a.Date.Date).Distinct().Count();
            var present = attendances.Count(a => a.Status == "Present");
            var absent = attendances.Count(a => a.Status == "Absent");
            var late = attendances.Count(a => a.Status == "Late");
            var excused = attendances.Count(a => a.Status == "Excused");

            var overallAttendanceRate = attendances.Any() 
                ? (double)present / attendances.Count * 100 
                : 0;

            return new ClassAttendanceSummaryDto
            {
                ClassId = classId,
                TotalStudents = totalStudents,
                TotalClasses = totalClasses,
                TotalAttendanceRecords = attendances.Count,
                Present = present,
                Absent = absent,
                Late = late,
                Excused = excused,
                OverallAttendanceRate = overallAttendanceRate
            };
        }
    }

    public class AttendanceSummaryDto
    {
        public Guid StudentId { get; set; }
        public Guid SemesterId { get; set; }
        public int TotalClasses { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int Excused { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class ClassAttendanceSummaryDto
    {
        public Guid ClassId { get; set; }
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalAttendanceRecords { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int Excused { get; set; }
        public double OverallAttendanceRate { get; set; }
    }
}