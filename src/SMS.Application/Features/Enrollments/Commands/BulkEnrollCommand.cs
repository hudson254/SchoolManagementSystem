using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Enrollments.Commands
{
    public class BulkEnrollCommand : IRequest<BulkEnrollmentDto>
    {
        public List<Guid> StudentIds { get; set; } = new();
        public Guid UnitId { get; set; }
        public Guid SemesterId { get; set; }
    }

    public class BulkEnrollCommandValidator : AbstractValidator<BulkEnrollCommand>
    {
        public BulkEnrollCommandValidator()
        {
            RuleFor(x => x.StudentIds)
                .NotEmpty().WithMessage("At least one student ID is required");

            RuleFor(x => x.UnitId)
                .NotEmpty().WithMessage("Unit ID is required");

            RuleFor(x => x.SemesterId)
                .NotEmpty().WithMessage("Semester ID is required");
        }
    }

    public class BulkEnrollCommandHandler : IRequestHandler<BulkEnrollCommand, BulkEnrollmentDto>
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<BulkEnrollCommandHandler> _logger;

        public BulkEnrollCommandHandler(
            IEnrollmentRepository enrollmentRepository,
            IStudentRepository studentRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<BulkEnrollCommandHandler> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _studentRepository = studentRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<BulkEnrollmentDto> Handle(BulkEnrollCommand request, CancellationToken cancellationToken)
        {
            var result = new BulkEnrollmentDto
            {
                StudentIds = request.StudentIds,
                UnitId = request.UnitId,
                SemesterId = request.SemesterId,
                Errors = new List<string>()
            };

            foreach (var studentId in request.StudentIds)
            {
                try
                {
                    var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
                    if (student == null)
                    {
                        result.TotalFailed++;
                        result.Errors.Add($"Student {studentId} not found");
                        continue;
                    }

                    var enrollment = new Enrollment
                    {
                        StudentId = studentId,
                        UnitId = request.UnitId,
                        SemesterId = request.SemesterId,
                        EnrollmentDate = DateTime.UtcNow,
                        Status = "Enrolled",
                        IsActive = true
                    };

                    await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
                    result.TotalEnrolled++;
                }
                catch (Exception ex)
                {
                    result.TotalFailed++;
                    result.Errors.Add($"Error enrolling student {studentId}: {ex.Message}");
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("Enrollment", "BulkEnroll", $"Enrolled {result.TotalEnrolled}, failed {result.TotalFailed}");

            _logger.LogInformation("Bulk enrollment: {Enrolled} enrolled, {Failed} failed", result.TotalEnrolled, result.TotalFailed);

            return result;
        }
    }
}
