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
    public class AttendanceRepository : BaseRepository<Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(ApplicationDbContext context, ILogger<AttendanceRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesByStudentAsync(Guid studentId)
        {
            return await _dbSet.Where(a => a.StudentId == studentId && !a.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesByClassAsync(Guid classId)
        {
            return await _dbSet.Where(a => a.ClassId == classId && !a.IsDeleted).ToListAsync();
        }

        public async Task<Attendance> GetAttendanceByStudentAndClassAsync(Guid studentId, Guid classId, DateTime date)
        {
            return await _dbSet.FirstOrDefaultAsync(a =>
                a.StudentId == studentId &&
                a.ClassId == classId &&
                a.Date.Date == date.Date &&
                !a.IsDeleted);
        }

        public async Task<IEnumerable<Attendance>> GetAttendanceByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet.Where(a => a.Date >= startDate && a.Date <= endDate && !a.IsDeleted).ToListAsync();
        }

        public async Task<int> CountAttendancesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(a => !a.IsDeleted, cancellationToken);
        }
    }
}

