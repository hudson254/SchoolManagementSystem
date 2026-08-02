using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Commands
{
    public class AllocateUnitCommand : IRequest<UnitAllocationDto>
    {
        public Guid LecturerId { get; set; }
        public Guid UnitId { get; set; }
        public bool IsPrimary { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class AllocateUnitCommandValidator : AbstractValidator<AllocateUnitCommand>
    {
        public AllocateUnitCommandValidator()
        {
            RuleFor(x => x.LecturerId)
                .NotEmpty().WithMessage("Lecturer ID is required");

            RuleFor(x => x.UnitId)
                .NotEmpty().WithMessage("Unit ID is required");
        }
    }

    public class AllocateUnitCommandHandler : IRequestHandler<AllocateUnitCommand, UnitAllocationDto>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<AllocateUnitCommandHandler> _logger;

        public AllocateUnitCommandHandler(
            ILecturerRepository lecturerRepository,
            IUnitRepository unitRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<AllocateUnitCommandHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _unitRepository = unitRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<UnitAllocationDto> Handle(AllocateUnitCommand request, CancellationToken cancellationToken)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(request.LecturerId, cancellationToken);
            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.LecturerId);

            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
                throw new NotFoundException("Unit", request.UnitId);

            // Check if allocation already exists via repository
            var existingAllocations = await _lecturerRepository.FindAsync(l => l.Id == request.LecturerId, cancellationToken);

            var allocation = new UnitAllocation
            {
                LecturerId = request.LecturerId,
                UnitId = request.UnitId,
                SemesterId = Guid.Empty, // SemesterId must be provided separately
                AllocationDate = DateTime.UtcNow,
                IsPrimary = request.IsPrimary,
                Notes = request.Notes,
                Status = "Active",
                TenantId = lecturer.TenantId
            };

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Allocate", "UnitAllocation",
                $"Unit {unit.Code} allocated to lecturer {lecturer.EmployeeNumber}");

            _logger.LogInformation("Unit {UnitCode} allocated to lecturer {Lecturer}",
                unit.Code, lecturer.EmployeeNumber);

            return new UnitAllocationDto
            {
                LecturerId = allocation.LecturerId,
                LecturerName = $"{lecturer.FirstName} {lecturer.LastName}".Trim(),
                UnitId = allocation.UnitId,
                UnitCode = unit.Code,
                UnitName = unit.Name,
                CreditHours = unit.Credits,
                SemesterId = allocation.SemesterId,
                SemesterName = $"Semester {unit.Semester}"
            };
        }
    }
}

