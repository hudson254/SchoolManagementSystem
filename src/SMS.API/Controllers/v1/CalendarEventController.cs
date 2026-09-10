using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/calendar-events")]
    public class CalendarEventController : BaseApiController
    {
        private readonly ILogger<CalendarEventController> _logger;
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CalendarEventController(
            ILogger<CalendarEventController> logger,
            ICalendarEventRepository calendarEventRepository,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _calendarEventRepository = calendarEventRepository;
            _unitOfWork = unitOfWork;
        }
[HttpGet]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<CalendarEventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? eventType = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<CalendarEvent> events;
            if (startDate.HasValue && endDate.HasValue)
                events = await _calendarEventRepository.GetEventsByDateRangeAsync(startDate.Value, endDate.Value);
            else if (!string.IsNullOrEmpty(eventType))
                events = await _calendarEventRepository.GetEventsByTypeAsync(eventType);
            else
                events = await _calendarEventRepository.GetAllAsync(cancellationToken);
            return Ok(events.Select(e => ToDto(e)));
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(CalendarEventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(Guid id, CancellationToken cancellationToken)
        {
            var evt = await _calendarEventRepository.GetByIdAsync(id, cancellationToken);
            if (evt == null) return NotFound();
            return Ok(ToDto(evt));
        }

        [HttpPost]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(CalendarEventDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEvent(
            [FromBody] CreateCalendarEventRequest request,
            CancellationToken cancellationToken)
        {
            var evt = new CalendarEvent
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                EventType = request.EventType ?? "other",
                Location = request.Location ?? string.Empty,
                IsActive = true
            };
            var result = await _calendarEventRepository.AddAsync(evt, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetEvent), new { id = result.Id }, ToDto(result));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ModeratorAccess")]
        [ProducesResponseType(typeof(CalendarEventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateCalendarEventRequest request, CancellationToken cancellationToken)
        {
            var evt = await _calendarEventRepository.GetByIdAsync(id, cancellationToken);
            if (evt == null) return NotFound();
            if (request.Title != null) evt.Title = request.Title;
            if (request.Description != null) evt.Description = request.Description;
            if (request.StartDate.HasValue) evt.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) evt.EndDate = request.EndDate.Value;
            if (request.EventType != null) evt.EventType = request.EventType;
            if (request.Location != null) evt.Location = request.Location;
            await _calendarEventRepository.UpdateAsync(evt, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Ok(ToDto(evt));
        }
[HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
        {
            var evt = await _calendarEventRepository.GetByIdAsync(id, cancellationToken);
            if (evt == null) return NotFound();
            await _calendarEventRepository.DeleteAsync(evt, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpGet("upcoming")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<CalendarEventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingEvents([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        {
            return Ok((await _calendarEventRepository.GetUpcomingEventsAsync(limit)).Select(e => ToDto(e)));
        }

        [HttpGet("range")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<CalendarEventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEventsInRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken cancellationToken = default)
        {
            return Ok((await _calendarEventRepository.GetEventsByDateRangeAsync(startDate, endDate)).Select(e => ToDto(e)));
        }

        [HttpGet("search")]
        [Authorize(Policy = "LecturerAccess")]
        [ProducesResponseType(typeof(IEnumerable<CalendarEventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchEvents([FromQuery] string searchTerm = "", CancellationToken cancellationToken = default)
        {
            var allEvents = await _calendarEventRepository.GetAllAsync(cancellationToken);
            var filtered = allEvents.Where(e =>
                e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            return Ok(filtered.Select(e => ToDto(e)));
        }

        private static CalendarEventDto ToDto(CalendarEvent e) => new CalendarEventDto
        {
            Id = e.Id, Title = e.Title, Description = e.Description,
            StartDate = e.StartDate, EndDate = e.EndDate, EventType = e.EventType,
            Location = e.Location, IsActive = e.IsActive, CreatedAt = e.CreatedDate ?? DateTime.UtcNow
        };
    }
}