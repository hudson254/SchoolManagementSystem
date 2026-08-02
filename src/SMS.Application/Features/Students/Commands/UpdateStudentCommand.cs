using MediatR;
using FluentValidation;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;

namespace SMS.Application.Features.Students.Commands
{
    public class UpdateStudentCommand : IRequest<StudentDto>
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public Guid? ProgrammeId { get; set; }
        public string? AcademicStatus { get; set; }
        public bool IsEnrolled { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
    }

    public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
    {
        public UpdateStudentCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Student ID is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .MaximumLength(20);

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required")
                .LessThan(DateTime.UtcNow.AddYears(-16))
                .WithMessage("Student must be at least 16 years old");
        }
    }

    public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, StudentDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserManagerService _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateStudentCommandHandler> _logger;

        public UpdateStudentCommandHandler(
            IStudentRepository studentRepository,
            IUserManagerService userManager,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateStudentCommandHandler> logger)
        {
            _studentRepository = studentRepository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<StudentDto> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetStudentWithDetailsAsync(request.Id, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.Id);
            }

            var user = student.User;

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;

            student.DateOfBirth = request.DateOfBirth;
            student.Gender = request.Gender;
            student.Address = request.Address;
            student.ProgrammeId = request.ProgrammeId;
            student.AcademicStatus = request.AcademicStatus ?? student.AcademicStatus;
            student.IsEnrolled = request.IsEnrolled;
            student.EmergencyContactName = request.EmergencyContactName;
            student.EmergencyContactPhone = request.EmergencyContactPhone;
            student.EmergencyContactRelation = request.EmergencyContactRelation;

            _studentRepository.Update(student);
            await _userManager.UpdateUserAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Student", "Update", student.Id, null, $"Updated student: {student.StudentNumber}");

            _logger.LogInformation("Student updated: {StudentNumber}", student.StudentNumber);

            return new StudentDto
            {
                Id = student.Id,
                UserId = user.Id,
                StudentNumber = student.StudentNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                DateOfBirth = student.DateOfBirth,
                Gender = student.Gender,
                Address = student.Address,
                EnrollmentDate = student.EnrollmentDate,
                ProgrammeId = student.ProgrammeId,
                AcademicStatus = student.AcademicStatus,
                IsEnrolled = student.IsEnrolled,
                CreatedDate = student.CreatedDate
            };
        }
    }
}