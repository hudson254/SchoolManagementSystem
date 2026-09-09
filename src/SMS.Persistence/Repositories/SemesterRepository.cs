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
    public class SemesterRepository : ISemesterRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SemesterRepository> _logger;

        public SemesterRepository(ApplicationDbContext context, ILogger<SemesterRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Semester>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Semesters
                .OrderByDescending(s => s.IsCurrent)
                .ThenByDescending(s => s.StartDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Semester?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Semesters
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Semester?> GetCurrentOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            var current = await _context.Semesters
                .Where(s => s.IsCurrent)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (current != null)
                return current;

            var active = await _context.Semesters
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (active != null)
                return active;

            return await _context.Semesters
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}