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
    public class LoginHistoryRepository : BaseRepository<LoginHistory>, ILoginHistoryRepository
    {
        public LoginHistoryRepository(ApplicationDbContext context, ILogger<LoginHistoryRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<LoginHistory>> GetByUserAsync(string userId)
        {
            return await _dbSet
                .Where(h => h.UserId == userId && !h.IsDeleted)
                .OrderByDescending(h => h.LoginTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<LoginHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(h => h.LoginTime >= startDate && h.LoginTime <= endDate && !h.IsDeleted)
                .OrderByDescending(h => h.LoginTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<LoginHistory>> GetRecentLoginsAsync(int count)
        {
            return await _dbSet
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.LoginTime)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetLoginCountByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(h =>
                h.UserId == userId &&
                h.IsSuccessful &&
                !h.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<LoginHistory>> GetFailedLoginsAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(h => !h.IsSuccessful && h.LoginTime >= since && !h.IsDeleted)
                .OrderByDescending(h => h.LoginTime)
                .ToListAsync(cancellationToken);
        }
    }
}
