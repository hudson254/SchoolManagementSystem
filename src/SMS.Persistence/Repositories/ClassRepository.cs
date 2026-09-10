using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Persistence.Repositories
{
    public class ClassRepository : BaseRepository<Class>, IClassRepository
    {
        public ClassRepository(ApplicationDbContext context, ILogger<ClassRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Class>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.Unit)
                .Include(c => c.Lecturer)
                .Include(c => c.Semester)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.StartDate)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
    }
}