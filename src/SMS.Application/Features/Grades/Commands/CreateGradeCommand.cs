using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Commands
{
    public class CreateGradeCommand : IRequest<GradeDto>
    {
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? SemesterId { get; set; }
        public decimal Score { get; set; }
        public string? Remarks { get; set; }
    }

    public class CreateGradeCommandValidator : AbstractValidator<CreateGradeCommand>
    {
        public CreateGradeCommandValidator()
        {
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("Student ID is required");
            RuleFor(x => x.UnitId).NotEmpty().WithMessage("Unit ID is required");
            RuleFor(x => x.Score).InclusiveBetween(0, 100).WithMessage("Score must be between 0 and 100");
        }
    }

    public class CreateGradeCommandHandler : IRequestHandler<CreateGradeCommand, GradeDto>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IAssessmentEngine _assessmentEngine;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<CreateGradeCommandHandler> _logger;

        public CreateGradeCommandHandler(
            IGradeRepository gradeRepository,
            IStudentRepository studentRepository,
            IAssessmentEngine assessmentEngine,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<CreateGradeCommandHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _studentRepository = studentRepository;
            _assessmentEngine = assessmentEngine;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<GradeDto> Handle(CreateGradeCommand request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
                throw new NotFoundException("Student", request.StudentId);

            var grade = new Grade
            {
                StudentId = request.StudentId,
                UnitId = request.UnitId,
                SemesterId = request.SemesterId,
                Score = request.Score,
                GradeValue = (await _assessmentEngine.AssignGradeAsync(request.Score, cancellationToken)).GradeLetter,
                Remarks = request.Remarks,
                GradedDate = DateTime.UtcNow,
                IsPublished = false
            };

            await _gradeRepository.AddAsync(grade, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Grade", "Create", grade.Id.ToString());

            _logger.LogInformation("Grade created for student {StudentId}, unit {UnitId}, score {Score}",
                request.StudentId, request.UnitId, request.Score);

            return new GradeDto
            {
                Id = grade.Id,
                StudentId = grade.StudentId,
                GradeValue = grade.GradeValue,
                Score = grade.Score,
                Remarks = grade.Remarks,
                GradedDate = grade.GradedDate,
                IsPublished = grade.IsPublished,
                StudentName = $"{student.FirstName} {student.LastName}",
                StudentNumber = student.StudentNumber,
                UnitName = grade.Unit?.Name ?? string.Empty,
                UnitCode = grade.Unit?.Code ?? string.Empty
            };
        }

    }
}
