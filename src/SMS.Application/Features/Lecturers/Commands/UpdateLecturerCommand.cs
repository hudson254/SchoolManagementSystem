using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Lecturers.Commands
{
    public class UpdateLecturerCommand : IRequest<LecturerDto>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? Specialization { get; set; }
        public string? Qualifications { get; set; }
    }

    public class UpdateLecturerCommandValidator : AbstractValidator<UpdateLecturerCommand>
    {
        public UpdateLecturerCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Lecturer ID is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100);
        }
    }

    public class UpdateLecturerCommandHandler : IRequestHandler<UpdateLecturerCommand, LecturerDto>
    {
        private readonly ILecturerRepository _lecturerRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateLecturerCommandHandler> _logger;

        public UpdateLecturerCommandHandler(
            ILecturerRepository lecturerRepository,
            IDepartmentRepository departmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateLecturerCommandHandler> logger)
        {
            _lecturerRepository = lecturerRepository;
            _departmentRepository = departmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<LecturerDto> Handle(UpdateLecturerCommand request, CancellationToken cancellationToken)
        {
            var lecturer = await _lecturerRepository.GetByIdAsync(request.Id, cancellationToken);
            if (lecturer == null)
                throw new NotFoundException("Lecturer", request.Id);

            // Validate department exists if changing
            if (request.DepartmentId.HasValue && request.DepartmentId != lecturer.DepartmentId)
            {
                var department = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
                if (department == null)
                    throw new NotFoundException("Department", request.DepartmentId.Value);
            }

            lecturer.FirstName = request.FirstName;
            lecturer.LastName = request.LastName;
            lecturer.PhoneNumber = request.PhoneNumber;
            lecturer.DepartmentId = request.DepartmentId;

            await _lecturerRepository.UpdateAsync(lecturer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Update", "Lecturer", $"Lecturer updated: {lecturer.EmployeeNumber}");

            _logger.LogInformation("Lecturer updated: {EmployeeNumber}", lecturer.EmployeeNumber);

            return new LecturerDto
            {
                Id = lecturer.Id,
                FirstName = lecturer.FirstName,
                LastName = lecturer.LastName,
                Email = lecturer.Email,
                PhoneNumber = lecturer.PhoneNumber,
                EmployeeNumber = lecturer.EmployeeNumber,
                DepartmentId = lecturer.DepartmentId,
                IsActive = lecturer.IsActive,
                UserId = lecturer.UserId?.ToString(),
                CreatedDate = lecturer.CreatedDate ?? DateTime.UtcNow
            };
        }
    }
}

