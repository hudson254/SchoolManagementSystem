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
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext context, ILogger<DepartmentRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<bool> IsDepartmentCodeUniqueAsync(string code, Guid? excludeId = null)
        {
            if (excludeId.HasValue)
                return !await _dbSet.AnyAsync(d => d.Code == code && d.Id != excludeId.Value && !d.IsDeleted);
            return !await _dbSet.AnyAsync(d => d.Code == code && !d.IsDeleted);
        }

        public async Task<Department> GetDepartmentWithCoursesAsync(Guid id)
        {
            return await _dbSet.Include(d => d.Courses)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        }

        public async Task<int> CountDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(d => !d.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(d => !d.IsDeleted).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Department>> GetActiveDepartmentsAsync()
        {
            return await _dbSet.Where(d => d.IsActive && !d.IsDeleted).ToListAsync();
        }

        public async Task<Department> GetDepartmentByCodeAsync(string code)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.Code == code && !d.IsDeleted);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsWithCoursesAsync()
        {
            return await _dbSet.Where(d => !d.IsDeleted).Include(d => d.Courses).ToListAsync();
        }

        public async Task<IEnumerable<Department>> SearchDepartmentsAsync(string searchTerm)
        {
            return await _dbSet.Where(d =>
                (d.Name.Contains(searchTerm) || d.Code.Contains(searchTerm)) &&
                !d.IsDeleted).ToListAsync();
        }
    }
}

