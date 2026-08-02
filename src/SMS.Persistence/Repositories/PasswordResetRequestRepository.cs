using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMS.Persistence.Repositories
{
    public class PasswordResetRequestRepository : IPasswordResetRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public PasswordResetRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetRequest?> GetByIdAsync(Guid id)
        {
            return await _context.PasswordResetRequests.FindAsync(id);
        }

        public async Task<IEnumerable<PasswordResetRequest>> GetPendingAsync()
        {
            return await _context.PasswordResetRequests
                .Where(r => r.Status == PasswordResetRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PasswordResetRequest>> GetAllAsync()
        {
            return await _context.PasswordResetRequests
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task AddAsync(PasswordResetRequest request)
        {
            await _context.PasswordResetRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PasswordResetRequest request)
        {
            _context.PasswordResetRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(PasswordResetRequest request)
        {
            _context.PasswordResetRequests.Remove(request);
            await _context.SaveChangesAsync();
        }
    }
}
