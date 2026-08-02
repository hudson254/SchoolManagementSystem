using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SMS.Application.Features.Timetables.Commands
{
    public class CreateTimetableCommand : IRequest<TimetableDto>
    {
        public Guid ClassId { get; set; }
        public Guid SemesterId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Venue { get; set; }
        public string? Topic { get; set; }
    }

    public class CreateTimetableHandler : IRequestHandler<CreateTimetableCommand, TimetableDto>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateTimetableHandler> _logger;

        public CreateTimetableHandler(
            ITimetableRepository timetableRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<TimetableDto> Handle(CreateTimetableCommand request, CancellationToken cancellationToken)
        {
            var timetable = new Timetable
            {
                ClassId = request.ClassId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                RoomNumber = request.Venue ?? string.Empty,
                IsActive = true
            };

            await _timetableRepository.AddAsync(timetable, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Timetable", "Create", timetable.Id.ToString());

            _logger.LogInformation("Timetable entry created for class {ClassId} on {Day}", request.ClassId, request.DayOfWeek);

            return new TimetableDto
            {
                Id = timetable.Id,
                ClassId = timetable.ClassId,
                DayOfWeek = timetable.DayOfWeek,
                StartTime = timetable.StartTime,
                EndTime = timetable.EndTime,
                Venue = timetable.RoomNumber,
                IsActive = timetable.IsActive
            };
        }
    }

    public class UpdateTimetableCommand : IRequest<TimetableDto>
    {
        public Guid Id { get; set; }
        public Guid ClassId { get; set; }
        public Guid SemesterId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Venue { get; set; }
        public string? Topic { get; set; }
    }

    public class UpdateTimetableHandler : IRequestHandler<UpdateTimetableCommand, TimetableDto>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateTimetableHandler> _logger;

        public UpdateTimetableHandler(
            ITimetableRepository timetableRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<TimetableDto> Handle(UpdateTimetableCommand request, CancellationToken cancellationToken)
        {
            var timetable = await _timetableRepository.GetByIdAsync(request.Id, cancellationToken);
            if (timetable == null)
                throw new NotFoundException("Timetable", request.Id);

            timetable.ClassId = request.ClassId;
            timetable.DayOfWeek = request.DayOfWeek;
            timetable.StartTime = request.StartTime;
            timetable.EndTime = request.EndTime;
            timetable.RoomNumber = request.Venue ?? string.Empty;

            await _timetableRepository.UpdateAsync(timetable, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Timetable", "Update", timetable.Id.ToString());

            _logger.LogInformation("Timetable {Id} updated", request.Id);

            return new TimetableDto
            {
                Id = timetable.Id,
                ClassId = timetable.ClassId,
                DayOfWeek = timetable.DayOfWeek,
                StartTime = timetable.StartTime,
                EndTime = timetable.EndTime,
                Venue = timetable.RoomNumber,
                IsActive = timetable.IsActive
            };
        }
    }

    public class DeleteTimetableCommand : IRequest<MediatR.Unit>
    {
        public Guid TimetableId { get; set; }
    }

    public class DeleteTimetableHandler : IRequestHandler<DeleteTimetableCommand, MediatR.Unit>
    {
        private readonly ITimetableRepository _timetableRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteTimetableHandler> _logger;

        public DeleteTimetableHandler(
            ITimetableRepository timetableRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteTimetableHandler> logger)
        {
            _timetableRepository = timetableRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeleteTimetableCommand request, CancellationToken cancellationToken)
        {
            var timetable = await _timetableRepository.GetByIdAsync(request.TimetableId, cancellationToken);
            if (timetable == null)
                throw new NotFoundException("Timetable", request.TimetableId);

            await _timetableRepository.DeleteAsync(timetable, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Timetable", "Delete", timetable.Id.ToString());

            _logger.LogInformation("Timetable {Id} deleted", request.TimetableId);
            return MediatR.Unit.Value;
        }
    }
}
