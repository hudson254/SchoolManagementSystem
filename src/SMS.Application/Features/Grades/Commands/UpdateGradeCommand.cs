using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Commands
{
    public class UpdateGradeCommand : IRequest<GradeDto>
    {
        public Guid Id { get; set; }
        public decimal Score { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateGradeCommandValidator : AbstractValidator<UpdateGradeCommand>
    {
        public UpdateGradeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Grade ID is required");
            RuleFor(x => x.Score).InclusiveBetween(0, 100).WithMessage("Score must be between 0 and 100");
        }
    }

    public class UpdateGradeCommandHandler : IRequestHandler<UpdateGradeCommand, GradeDto>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<UpdateGradeCommandHandler> _logger;

        public UpdateGradeCommandHandler(
            IGradeRepository gradeRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<UpdateGradeCommandHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<GradeDto> Handle(UpdateGradeCommand request, CancellationToken cancellationToken)
        {
            var grade = await _gradeRepository.GetByIdAsync(request.Id, cancellationToken);
            if (grade == null)
                throw new NotFoundException("Grade", request.Id);

            grade.Score = request.Score;
            grade.GradeValue = CalculateLetterGrade(request.Score);
            grade.Remarks = request.Remarks;

            await _gradeRepository.UpdateAsync(grade, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("Grade", "Update", grade.Id.ToString());

            _logger.LogInformation("Grade {GradeId} updated with score {Score}", request.Id, request.Score);

            return new GradeDto
            {
                Id = grade.Id,
                StudentId = grade.StudentId,
                EnrollmentId = grade.EnrollmentId ?? Guid.Empty,
                GradeValue = grade.GradeValue,
                Score = grade.Score,
                Remarks = grade.Remarks,
                GradedDate = grade.GradedDate,
                IsPublished = grade.IsPublished,
                PublishedDate = grade.PublishedDate,
                StudentName = grade.Student != null ? $"{grade.Student.FirstName} {grade.Student.LastName}" : string.Empty,
                StudentNumber = grade.Student?.StudentNumber ?? string.Empty,
                UnitName = grade.Unit?.Name ?? string.Empty,
                UnitCode = grade.Unit?.Code ?? string.Empty,
                Credits = grade.Unit?.Credits ?? 0
            };
        }

        private static string CalculateLetterGrade(decimal score)
        {
            return score switch
            {
                >= 80 => "A",
                >= 75 => "A-",
                >= 70 => "B+",
                >= 65 => "B",
                >= 60 => "B-",
                >= 55 => "C+",
                >= 50 => "C",
                >= 45 => "C-",
                >= 40 => "D+",
                >= 35 => "D",
                >= 30 => "D-",
                >= 25 => "E",
                _ => "F"
            };
        }
    }
}
