using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IPasswordResetRequestRepository
    {
        Task<PasswordResetRequest?> GetByIdAsync(Guid id);
        Task<IEnumerable<PasswordResetRequest>> GetPendingAsync();
        Task<IEnumerable<PasswordResetRequest>> GetAllAsync();
        Task AddAsync(PasswordResetRequest request);
        Task UpdateAsync(PasswordResetRequest request);
        Task DeleteAsync(PasswordResetRequest request);
    }
}
