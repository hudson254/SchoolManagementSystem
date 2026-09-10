using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Classes.Commands
{
    public class CreateClassCommand : IRequest<ClassDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid UnitId { get; set; }
        public Guid LecturerId { get; set; }
        public Guid SemesterId { get; set; }
        public int MaxCapacity { get; set; } = 50;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? ScheduleDay { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateClassCommand : IRequest<ClassDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid UnitId { get; set; }
        public Guid LecturerId { get; set; }
        public Guid SemesterId { get; set; }
        public int MaxCapacity { get; set; } = 50;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? ScheduleDay { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DeleteClassCommand : IRequest<MediatR.Unit>
    {
        public Guid ClassId { get; set; }
    }

    public class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
    {
        public CreateClassCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Class name is required").MaximumLength(100);
            RuleFor(x => x.Code).NotEmpty().WithMessage("Class code is required").MaximumLength(20);
            RuleFor(x => x.UnitId).NotEmpty().WithMessage("Unit is required");
            RuleFor(x => x.LecturerId).NotEmpty().WithMessage("Lecturer is required");
            RuleFor(x => x.SemesterId).NotEmpty().WithMessage("Semester is required");
            RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required");
            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");
            RuleFor(x => x.MaxCapacity).GreaterThan(0).WithMessage("Capacity must be greater than zero");
            RuleFor(x => x.ScheduleDay)
                .Must(ClassScheduleValidators.IsValidDay)
                .WithMessage("Schedule day must be a valid day of the week")
                .When(x => !string.IsNullOrWhiteSpace(x.ScheduleDay));
            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time")
                .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
        }
    }

    public class UpdateClassCommandValidator : AbstractValidator<UpdateClassCommand>
    {
        public UpdateClassCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Class ID is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Class name is required").MaximumLength(100);
            RuleFor(x => x.Code).NotEmpty().WithMessage("Class code is required").MaximumLength(20);
            RuleFor(x => x.UnitId).NotEmpty().WithMessage("Unit is required");
            RuleFor(x => x.LecturerId).NotEmpty().WithMessage("Lecturer is required");
            RuleFor(x => x.SemesterId).NotEmpty().WithMessage("Semester is required");
            RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required");
            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");
            RuleFor(x => x.MaxCapacity).GreaterThan(0).WithMessage("Capacity must be greater than zero");
            RuleFor(x => x.ScheduleDay)
                .Must(ClassScheduleValidators.IsValidDay)
                .WithMessage("Schedule day must be a valid day of the week")
                .When(x => !string.IsNullOrWhiteSpace(x.ScheduleDay));
            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time")
                .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
        }
    }

    internal static class ClassScheduleValidators
    {
        private static readonly string[] ValidDays =
        {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        };

        public static bool IsValidDay(string day)
        {
            return ValidDays.Contains(day, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, ClassDto>
    {
        private readonly IClassRepository _classRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateClassCommandHandler> _logger;

        public CreateClassCommandHandler(
            IClassRepository classRepository,
            IUnitRepository unitRepository,
            ILecturerRepository lecturerRepository,
            ISemesterRepository semesterRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateClassCommandHandler> logger)
        {
            _classRepository = classRepository;
            _unitRepository = unitRepository;
            _lecturerRepository = lecturerRepository;
            _semesterRepository = semesterRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ClassDto> Handle(CreateClassCommand request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
                throw new NotFoundException("Unit", request.UnitId);

            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            var semester = await _semesterRepository.GetByIdAsync(request.SemesterId, cancellationToken);
            if (semester == null)
                throw new NotFoundException("Semester", request.SemesterId);

            var klass = new Class
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                UnitId = request.UnitId,
                LecturerId = request.LecturerId,
                SemesterId = request.SemesterId,
                MaxCapacity = request.MaxCapacity,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ScheduleDay = request.ScheduleDay,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsActive = request.IsActive
            };

            await _classRepository.AddAsync(klass, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("Class", "Create", klass.Id.ToString());
            _logger.LogInformation("Class created: {Code} ({Name})", klass.Code, klass.Name);

            return ClassDtoMapper.ToDto(klass, unit, lecturer, semester);
        }
    }

    public class UpdateClassCommandHandler : IRequestHandler<UpdateClassCommand, ClassDto>
    {
        private readonly IClassRepository _classRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILecturerRepository _lecturerRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateClassCommandHandler> _logger;

        public UpdateClassCommandHandler(
            IClassRepository classRepository,
            IUnitRepository unitRepository,
            ILecturerRepository lecturerRepository,
            ISemesterRepository semesterRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateClassCommandHandler> logger)
        {
            _classRepository = classRepository;
            _unitRepository = unitRepository;
            _lecturerRepository = lecturerRepository;
            _semesterRepository = semesterRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<ClassDto> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
        {
            var klass = await _classRepository.GetByIdAsync(request.Id, cancellationToken);
            if (klass == null)
                throw new NotFoundException("Class", request.Id);

            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
                throw new NotFoundException("Unit", request.UnitId);

            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            var semester = await _semesterRepository.GetByIdAsync(request.SemesterId, cancellationToken);
            if (semester == null)
                throw new NotFoundException("Semester", request.SemesterId);

            klass.Name = request.Name;
            klass.Code = request.Code;
            klass.Description = request.Description;
            klass.UnitId = request.UnitId;
            klass.LecturerId = request.LecturerId;
            klass.SemesterId = request.SemesterId;
            klass.MaxCapacity = request.MaxCapacity;
            klass.StartDate = request.StartDate;
            klass.EndDate = request.EndDate;
            klass.ScheduleDay = request.ScheduleDay;
            klass.StartTime = request.StartTime;
            klass.EndTime = request.EndTime;
            klass.IsActive = request.IsActive;

            await _classRepository.UpdateAsync(klass, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("Class", "Update", klass.Id.ToString());
            _logger.LogInformation("Class {Id} updated", klass.Id);

            return ClassDtoMapper.ToDto(klass, unit, lecturer, semester);
        }
    }

    public class DeleteClassCommandHandler : IRequestHandler<DeleteClassCommand, MediatR.Unit>
    {
        private readonly IClassRepository _classRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<DeleteClassCommandHandler> _logger;

        public DeleteClassCommandHandler(
            IClassRepository classRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<DeleteClassCommandHandler> logger)
        {
            _classRepository = classRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<MediatR.Unit> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
        {
            var klass = await _classRepository.GetByIdAsync(request.ClassId, cancellationToken);
            if (klass == null)
                throw new NotFoundException("Class", request.ClassId);

            await _classRepository.DeleteAsync(klass, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("Class", "Delete", klass.Id.ToString());
            _logger.LogInformation("Class {Id} deleted", request.ClassId);
            return MediatR.Unit.Value;
        }
    }

    internal static class ClassDtoMapper
    {
        public static ClassDto ToDto(Class klass, Domain.Entities.Unit? unit, Lecturer? lecturer, Semester? semester)
        {
            var lecturerName = lecturer != null
                ? $"{lecturer.FirstName} {lecturer.LastName}".Trim()
                : string.Empty;

            return new ClassDto
            {
                Id = klass.Id,
                Name = klass.Name,
                Code = klass.Code,
                Description = klass.Description,
                UnitId = klass.UnitId,
                UnitName = unit?.Name ?? klass.Unit?.Name ?? string.Empty,
                UnitCode = unit?.Code ?? klass.Unit?.Code ?? string.Empty,
                LecturerId = klass.LecturerId,
                LecturerName = lecturerName,
                LecturerEmail = lecturer?.Email ?? string.Empty,
                SemesterId = klass.SemesterId,
                SemesterName = semester?.Name ?? klass.Semester?.Name ?? string.Empty,
                MaxCapacity = klass.MaxCapacity,
                CurrentEnrollment = klass.CurrentEnrollment,
                StartDate = klass.StartDate,
                EndDate = klass.EndDate,
                ScheduleDay = klass.ScheduleDay,
                StartTime = klass.StartTime,
                EndTime = klass.EndTime,
                IsActive = klass.IsActive,
                CreatedDate = klass.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}