using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands
{
    public class CreateEnrollmentCommand : IRequest<EnrollmentDto>
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid UnitId { get; set; }
        public Guid SemesterId { get; set; }
        public string? Status { get; set; }
    }

    public class CreateEnrollmentCommandValidator : AbstractValidator<CreateEnrollmentCommand>
    {
        public CreateEnrollmentCommandValidator()
        {
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("Student ID is required");

            RuleFor(x => x.CourseId)
                .NotEmpty().WithMessage("Course ID is required");

            RuleFor(x => x.UnitId)
                .NotEmpty().WithMessage("Unit ID is required");

            RuleFor(x => x.SemesterId)
                .NotEmpty().WithMessage("Semester ID is required");
        }
    }

    public class CreateEnrollmentCommandHandler : IRequestHandler<CreateEnrollmentCommand, EnrollmentDto>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateEnrollmentCommandHandler> _logger;

        public CreateEnrollmentCommandHandler(
            IEnrollmentRepository enrollmentRepository,
            IStudentRepository studentRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateEnrollmentCommandHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _studentRepository = studentRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<EnrollmentDto> Handle(CreateEnrollmentCommand request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
                throw new NotFoundException("Student", request.StudentId);

            var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
            if (course == null)
                throw new NotFoundException("Course", request.CourseId);

            var existingEnrollments = await _enrollmentRepository.GetEnrollmentsAsync(cancellationToken);
            var duplicate = existingEnrollments.FirstOrDefault(e =>
                e.StudentId == request.StudentId &&
                e.UnitId == request.UnitId &&
                e.SemesterId == request.SemesterId &&
                e.Status != "Dropped");
            if (duplicate != null)
                throw new ConflictException("Student is already enrolled in this unit for the semester");

            var enrollment = new Enrollment
            {
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                UnitId = request.UnitId,
                SemesterId = request.SemesterId,
                EnrollmentDate = DateTime.UtcNow,
                Status = request.Status ?? "Enrolled",
                IsActive = true
            };

            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Enrollment", "Create", enrollment.Id.ToString());

            _logger.LogInformation("Enrollment created: Student {StudentId} enrolled in Unit {UnitId}", request.StudentId, request.UnitId);

            return new EnrollmentDto
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                UnitId = enrollment.UnitId ?? Guid.Empty,
                SemesterId = enrollment.SemesterId ?? Guid.Empty,
                EnrollmentDate = enrollment.EnrollmentDate,
                Status = enrollment.Status,
                StudentName = $"{student.FirstName} {student.LastName}",
                StudentNumber = student.StudentNumber,
                UnitName = enrollment.Unit?.Name ?? string.Empty,
                UnitCode = enrollment.Unit?.Code ?? string.Empty,
                Credits = 0,
                SemesterName = string.Empty
            };
        }
    }
}
