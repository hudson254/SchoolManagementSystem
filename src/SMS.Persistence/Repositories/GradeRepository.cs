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
    public class GradeRepository : BaseRepository<Grade>, IGradeRepository
    {
        public GradeRepository(ApplicationDbContext context, ILogger<GradeRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Grade>> GetGradesByStudentAsync(Guid studentId)
        {
            return await _dbSet.Where(g => g.StudentId == studentId && !g.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Grade>> GetGradesByUnitAsync(Guid unitId)
        {
            return await _dbSet.Where(g => g.UnitId == unitId && !g.IsDeleted).ToListAsync();
        }

        public async Task<double> GetStudentGPAAsync(Guid studentId)
        {
            var grades = await _dbSet.Where(g => g.StudentId == studentId && !g.IsDeleted).ToListAsync();
            if (grades.Count == 0) return 0.0;
            return (double)grades.Average(g => g.Score);
        }

        public async Task<IEnumerable<Grade>> GetStudentGradesAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(g => g.StudentId == studentId && !g.IsDeleted)
                .Include(g => g.Unit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetGradesForSemesterAsync(Guid semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(g => !g.IsDeleted)
                .Include(g => g.Student)
                .Include(g => g.Unit)
                .Where(g => g.Student.CurrentSemesterId == semesterId)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountGradesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(g => !g.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Grade>> GetAllGradesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(g => !g.IsDeleted)
                .Include(g => g.Student)
                .Include(g => g.Unit)
                .ToListAsync(cancellationToken);
        }
    }
}

