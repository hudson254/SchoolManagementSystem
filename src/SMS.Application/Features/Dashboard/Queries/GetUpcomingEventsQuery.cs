using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Dashboard.Queries
{
    public class GetUpcomingEventsQuery : IRequest<IEnumerable<EventDto>>
    {
        public int Days { get; set; } = 30;
    }

    public class GetUpcomingEventsQueryHandler : IRequestHandler<GetUpcomingEventsQuery, IEnumerable<EventDto>>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<GetUpcomingEventsQueryHandler> _logger;

        public GetUpcomingEventsQueryHandler(
            ICalendarEventRepository calendarEventRepository,
            IAssignmentRepository assignmentRepository,
            ILogger<GetUpcomingEventsQueryHandler> logger)
        {
            _calendarEventRepository = calendarEventRepository;
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<EventDto>> Handle(GetUpcomingEventsQuery request, CancellationToken cancellationToken)
        {
            var events = new List<EventDto>();

            // Calendar events
            var calendarEvents = await _calendarEventRepository.GetUpcomingEventsAsync(request.Days);
            events.AddRange(calendarEvents.Select(e => new EventDto
            {
                Title = e.Title,
                Description = e.Description,
                Date = e.StartDate,
                Time = $"{e.StartDate:HH:mm} - {e.EndDate:HH:mm}",
                Location = e.Location,
                EventType = e.EventType,
                Color = "#576426"
            }));

            // Assignment deadlines
            var assignments = await _assignmentRepository.GetUpcomingDeadlinesAsync(Guid.Empty, request.Days, cancellationToken);
            events.AddRange(assignments.Select(a => new EventDto
            {
                Title = $"Assignment Due: {a.Title}",
                Description = a.Description,
                Date = a.DueDate,
                Time = a.DueDate.ToString("HH:mm"),
                Location = "Online",
                EventType = "Assignment",
                Color = "#f44336"
            }));

            return events.OrderBy(e => e.Date).Take(20);
        }
    }
}




