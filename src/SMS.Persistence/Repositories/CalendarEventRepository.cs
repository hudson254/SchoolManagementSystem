using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class CalendarEventRepository : BaseRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(ApplicationDbContext context, ILogger<CalendarEventRepository> logger) 
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet.Where(e => e.StartDate >= startDate && e.EndDate <= endDate && !e.IsDeleted)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CalendarEvent>> GetEventsByTypeAsync(string eventType)
        {
            return await _dbSet.Where(e => e.EventType == eventType && !e.IsDeleted)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(int count)
        {
            return await _dbSet.Where(e => e.StartDate >= DateTime.UtcNow && !e.IsDeleted)
                .OrderBy(e => e.StartDate)
                .Take(count)
                .ToListAsync();
        }
    }
}

