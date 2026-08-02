using MediatR;
using FluentValidation;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Students.Commands
{
    public class EnrollStudentCommand : IRequest
    {
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public Guid SemesterId { get; set; }
    }

    public class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
    {
        public EnrollStudentCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required");

            RuleFor(x => x.UnitId)
                .NotEmpty().WithMessage("Unit ID is required");

            RuleFor(x => x.SemesterId)
                .NotEmpty().WithMessage("Semester ID is required");
        }
    }

    public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<EnrollStudentCommandHandler> _logger;

        public EnrollStudentCommandHandler(
            IStudentRepository studentRepository,
            IUnitRepository unitRepository,
            IEnrollmentRepository enrollmentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<EnrollStudentCommandHandler> logger)
        {
            _studentRepository = studentRepository;
            _unitRepository = unitRepository;
            _enrollmentRepository = enrollmentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
            {
                throw new NotFoundException("Unit", request.UnitId);
            }

            var existingEnrollment = await _enrollmentRepository.GetEnrollmentAsync(
                request.StudentId,
                request.UnitId,
                request.SemesterId,
                cancellationToken);

            if (existingEnrollment != null)
            {
                throw new ConflictException("Enrollment", "Student-Unit-Semester", $"{request.StudentId}-{request.UnitId}-{request.SemesterId}");
            }

            var enrollment = new StudentEnrollment
            {
                StudentId = request.StudentId,
                UnitId = request.UnitId,
                SemesterId = request.SemesterId,
                EnrollmentDate = DateTime.UtcNow,
                Status = "Enrolled"
            };

            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Enrollment", "Create", enrollment.Id, null, $"Student: {student.StudentNumber}, Unit: {unit.Code}");

            _logger.LogInformation("Student {StudentNumber} enrolled in {UnitCode}", student.StudentNumber, unit.Code);
        }
    }
}