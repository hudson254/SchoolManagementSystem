using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ILoginHistoryRepository : IRepository<LoginHistory>
    {
        Task<IEnumerable<LoginHistory>> GetByUserAsync(string userId);
        Task<IEnumerable<LoginHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<LoginHistory>> GetRecentLoginsAsync(int count);
        Task<int> GetLoginCountByUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<LoginHistory>> GetFailedLoginsAsync(DateTime since, CancellationToken cancellationToken = default);
    }
}
