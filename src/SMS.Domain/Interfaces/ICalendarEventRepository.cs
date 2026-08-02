using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ICalendarEventRepository : IRepository<CalendarEvent>
    {
        Task<IEnumerable<CalendarEvent>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CalendarEvent>> GetEventsByTypeAsync(string eventType);
        Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(int count);
    }
}