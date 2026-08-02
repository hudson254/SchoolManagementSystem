using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SMS.Application.Features.Timetables.Queries
{
    public class GetTimetablesQuery : IRequest<PagedResult<TimetableDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? ClassId { get; set; }
        public Guid? SemesterId { get; set; }
        public string? DayOfWeek { get; set; }
    }

    public class GetTimetablesHandler : IRequestHandler<GetTimetablesQuery, PagedResult<TimetableDto>>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetTimetablesHandler> _logger;

        public GetTimetablesHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetTimetablesHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<PagedResult<TimetableDto>> Handle(GetTimetablesQuery request, CancellationToken cancellationToken)
        {
            var all = await _timetableRepository.GetAllAsync(cancellationToken);
            var list = all.Where(t => !t.IsDeleted).AsEnumerable();

            if (request.ClassId.HasValue)
                list = list.Where(t => t.ClassId == request.ClassId.Value);
            if (!string.IsNullOrWhiteSpace(request.DayOfWeek))
                list = list.Where(t => t.DayOfWeek.Equals(request.DayOfWeek, StringComparison.OrdinalIgnoreCase));

            var orderedList = list.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).ToList();
            var totalCount = orderedList.Count;

            var pagedItems = orderedList
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TimetableDto
                {
                    Id = t.Id,
                    ClassId = t.ClassId,
                    DayOfWeek = t.DayOfWeek,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    Venue = t.RoomNumber,
                    IsActive = t.IsActive,
                    UnitName = t.Unit != null ? t.Unit.Name : string.Empty,
                    UnitCode = t.Unit != null ? t.Unit.Code : string.Empty,
                    LecturerName = t.Lecturer != null ? $"{t.Lecturer.FirstName} {t.Lecturer.LastName}" : string.Empty
                })
                .ToList();

            return new PagedResult<TimetableDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = request.Page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }

    public class GetTimetableQuery : IRequest<TimetableDto>
    {
        public Guid TimetableId { get; set; }
    }

    public class GetTimetableHandler : IRequestHandler<GetTimetableQuery, TimetableDto>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetTimetableHandler> _logger;

        public GetTimetableHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<TimetableDto> Handle(GetTimetableQuery request, CancellationToken cancellationToken)
        {
            var timetable = await _timetableRepository.GetByIdAsync(request.TimetableId, cancellationToken);
            if (timetable == null)
                throw new NotFoundException("Timetable", request.TimetableId);

            return new TimetableDto
            {
                Id = timetable.Id,
                ClassId = timetable.ClassId,
                DayOfWeek = timetable.DayOfWeek,
                StartTime = timetable.StartTime,
                EndTime = timetable.EndTime,
                Venue = timetable.RoomNumber,
                IsActive = timetable.IsActive,
                UnitName = timetable.Unit != null ? timetable.Unit.Name : string.Empty,
                UnitCode = timetable.Unit != null ? timetable.Unit.Code : string.Empty,
                LecturerName = timetable.Lecturer != null ? $"{timetable.Lecturer.FirstName} {timetable.Lecturer.LastName}" : string.Empty
            };
        }
    }

    public class GetClassTimetableQuery : IRequest<IEnumerable<TimetableDto>>
    {
        public Guid? ClassId { get; set; }
        public Guid? CourseId { get; set; }
        public string? DayOfWeek { get; set; }
    }

    public class GetClassTimetableHandler : IRequestHandler<GetClassTimetableQuery, IEnumerable<TimetableDto>>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetClassTimetableHandler> _logger;

        public GetClassTimetableHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetClassTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<TimetableDto>> Handle(GetClassTimetableQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<Domain.Entities.Timetable> timetables;
            if (request.ClassId.HasValue)
                timetables = await _timetableRepository.GetTimetableByClassAsync(request.ClassId.Value);
            else
                timetables = await _timetableRepository.GetAllAsync(cancellationToken);

            var list = timetables.Where(t => !t.IsDeleted).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.DayOfWeek))
                list = list.Where(t => t.DayOfWeek.Equals(request.DayOfWeek, StringComparison.OrdinalIgnoreCase));

            return list.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).Select(t => new TimetableDto
            {
                Id = t.Id,
                ClassId = t.ClassId,
                DayOfWeek = t.DayOfWeek,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Venue = t.RoomNumber,
                IsActive = t.IsActive,
                UnitName = t.Unit != null ? t.Unit.Name : string.Empty,
                UnitCode = t.Unit != null ? t.Unit.Code : string.Empty,
                LecturerName = t.Lecturer != null ? $"{t.Lecturer.FirstName} {t.Lecturer.LastName}" : string.Empty
            });
        }
    }

    public class GetLecturerTimetableQuery : IRequest<IEnumerable<TimetableDto>>
    {
        public Guid LecturerId { get; set; }
        public Guid SemesterId { get; set; }
        public string? DayOfWeek { get; set; }
    }

    public class GetLecturerTimetableHandler : IRequestHandler<GetLecturerTimetableQuery, IEnumerable<TimetableDto>>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetLecturerTimetableHandler> _logger;

        public GetLecturerTimetableHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetLecturerTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<TimetableDto>> Handle(GetLecturerTimetableQuery request, CancellationToken cancellationToken)
        {
            var timetables = await _timetableRepository.GetTimetableByLecturerAsync(request.LecturerId);
            var list = timetables.Where(t => !t.IsDeleted).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.DayOfWeek))
                list = list.Where(t => t.DayOfWeek.Equals(request.DayOfWeek, StringComparison.OrdinalIgnoreCase));

            return list.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).Select(t => new TimetableDto
            {
                Id = t.Id,
                ClassId = t.ClassId,
                DayOfWeek = t.DayOfWeek,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Venue = t.RoomNumber,
                IsActive = t.IsActive,
                UnitName = t.Unit != null ? t.Unit.Name : string.Empty,
                UnitCode = t.Unit != null ? t.Unit.Code : string.Empty,
                LecturerName = t.Lecturer != null ? $"{t.Lecturer.FirstName} {t.Lecturer.LastName}" : string.Empty
            });
        }
    }

    public class GetStudentTimetableQuery : IRequest<IEnumerable<TimetableDto>>
    {
        public Guid StudentId { get; set; }
        public Guid SemesterId { get; set; }
        public string? DayOfWeek { get; set; }
    }

    public class GetStudentTimetableHandler : IRequestHandler<GetStudentTimetableQuery, IEnumerable<TimetableDto>>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetStudentTimetableHandler> _logger;

        public GetStudentTimetableHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetStudentTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<TimetableDto>> Handle(GetStudentTimetableQuery request, CancellationToken cancellationToken)
        {
            var timetables = await _timetableRepository.GetTimetableByStudentAsync(request.StudentId, cancellationToken);
            var list = timetables.Where(t => !t.IsDeleted).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.DayOfWeek))
                list = list.Where(t => t.DayOfWeek.Equals(request.DayOfWeek, StringComparison.OrdinalIgnoreCase));

            return list.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).Select(t => new TimetableDto
            {
                Id = t.Id,
                ClassId = t.ClassId,
                DayOfWeek = t.DayOfWeek,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Venue = t.RoomNumber,
                IsActive = t.IsActive,
                UnitName = t.Unit != null ? t.Unit.Name : string.Empty,
                UnitCode = t.Unit != null ? t.Unit.Code : string.Empty,
                LecturerName = t.Lecturer != null ? $"{t.Lecturer.FirstName} {t.Lecturer.LastName}" : string.Empty
            });
        }
    }

    public class GetWeeklyTimetableQuery : IRequest<WeeklyTimetableDto>
    {
        public DateTime WeekStartDate { get; set; }
        public Guid? ClassId { get; set; }
    }

    public class GetWeeklyTimetableHandler : IRequestHandler<GetWeeklyTimetableQuery, WeeklyTimetableDto>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetWeeklyTimetableHandler> _logger;

        public GetWeeklyTimetableHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetWeeklyTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<WeeklyTimetableDto> Handle(GetWeeklyTimetableQuery request, CancellationToken cancellationToken)
        {
            var weekEnd = request.WeekStartDate.AddDays(6);
            var timetables = await _timetableRepository.GetTimetableByDateRangeAsync(request.WeekStartDate, weekEnd, cancellationToken);
            var filteredTimetables = timetables.Where(t => !t.IsDeleted).ToList();

            var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var days = daysOfWeek.Select((day, index) => new DailyTimetableDto
            {
                Day = day,
                Date = request.WeekStartDate.AddDays(index),
                Entries = filteredTimetables
                    .Where(t => t.DayOfWeek.Equals(day, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.StartTime)
                    .Select(t => new TimetableDto
                    {
                        Id = t.Id,
                        ClassId = t.ClassId,
                        DayOfWeek = t.DayOfWeek,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                        Venue = t.RoomNumber,
                        IsActive = t.IsActive,
                        UnitName = t.Unit != null ? t.Unit.Name : string.Empty,
                        UnitCode = t.Unit != null ? t.Unit.Code : string.Empty,
                        LecturerName = t.Lecturer != null ? $"{t.Lecturer.FirstName} {t.Lecturer.LastName}" : string.Empty
                    }).ToList()
            }).ToList();

            return new WeeklyTimetableDto
            {
                WeekStartDate = request.WeekStartDate.ToString("yyyy-MM-dd"),
                WeekEndDate = weekEnd.ToString("yyyy-MM-dd"),
                Days = days
            };
        }
    }

    public class GetAvailableVenuesQuery : IRequest<IEnumerable<string>>
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public Guid SemesterId { get; set; }
    }

    public class GetAvailableVenuesHandler : IRequestHandler<GetAvailableVenuesQuery, IEnumerable<string>>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<GetAvailableVenuesHandler> _logger;

        public GetAvailableVenuesHandler(
            ITimetableRepository timetableRepository,
            ILogger<GetAvailableVenuesHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> Handle(GetAvailableVenuesQuery request, CancellationToken cancellationToken)
        {
            var all = await _timetableRepository.GetAllAsync(cancellationToken);

            var bookedRooms = all
                .Where(t => t.DayOfWeek.Equals(request.DayOfWeek, StringComparison.OrdinalIgnoreCase)
                    && t.StartTime < request.EndTime
                    && t.EndTime > request.StartTime
                    && !t.IsDeleted)
                .Select(t => t.RoomNumber)
                .Distinct()
                .ToList();

            var allRooms = new[] { "Room 101", "Room 102", "Room 103", "Room 201", "Room 202", "Lab A", "Lab B", "Lecture Hall 1", "Lecture Hall 2" };
            var available = allRooms.Where(r => !bookedRooms.Contains(r)).ToList();

            _logger.LogInformation("Found {Count} available venues for {Day} at {Start}-{End}", available.Count, request.DayOfWeek, request.StartTime, request.EndTime);
            return available;
        }
    }

    public class CheckTimetableConflictsQuery : IRequest<ConflictCheckResultDto> { }

    public class CheckTimetableConflictsHandler : IRequestHandler<CheckTimetableConflictsQuery, ConflictCheckResultDto>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly ILogger<CheckTimetableConflictsHandler> _logger;

        public CheckTimetableConflictsHandler(
            ITimetableRepository timetableRepository,
            ILogger<CheckTimetableConflictsHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _logger = logger;
        }

        public async Task<ConflictCheckResultDto> Handle(CheckTimetableConflictsQuery request, CancellationToken cancellationToken)
        {
            var all = await _timetableRepository.GetAllAsync(cancellationToken);
            var active = all.Where(t => !t.IsDeleted).ToList();
            var conflictList = new List<string>();

            var roomGroups = active.GroupBy(t => new { t.RoomNumber, t.DayOfWeek });
            foreach (var group in roomGroups)
            {
                var entries = group.OrderBy(t => t.StartTime).ToList();
                for (int i = 0; i < entries.Count - 1; i++)
                {
                    for (int j = i + 1; j < entries.Count; j++)
                    {
                        if (entries[i].StartTime < entries[j].EndTime && entries[j].StartTime < entries[i].EndTime)
                        {
                            conflictList.Add($"Room {group.Key.RoomNumber} on {group.Key.DayOfWeek}: {entries[i].StartTime}-{entries[i].EndTime} conflicts with {entries[j].StartTime}-{entries[j].EndTime}");
                        }
                    }
                }
            }

            _logger.LogInformation("Timetable conflict check: {ConflictCount} conflicts found", conflictList.Count);
            return new ConflictCheckResultDto
            {
                HasConflicts = conflictList.Any(),
                Conflicts = conflictList
            };
        }
    }
}
