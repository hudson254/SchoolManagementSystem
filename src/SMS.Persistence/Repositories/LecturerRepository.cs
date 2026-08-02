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
    public class LecturerRepository : BaseRepository<Lecturer>, ILecturerRepository
    {
        public LecturerRepository(ApplicationDbContext context, ILogger<LecturerRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Lecturer>> GetLecturersByDepartmentAsync(Guid departmentId)
        {
            return await _dbSet.Where(l => l.DepartmentId == departmentId && !l.IsDeleted).ToListAsync();
        }

        public async Task<IEnumerable<Lecturer>> GetActiveLecturersAsync()
        {
            return await _dbSet.Where(l => l.IsActive && !l.IsDeleted).ToListAsync();
        }

        public async Task<Lecturer> GetLecturerByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(l => l.Email == email && !l.IsDeleted);
        }

        public async Task<int> CountLecturersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(l => !l.IsDeleted, cancellationToken);
        }
    }
}

