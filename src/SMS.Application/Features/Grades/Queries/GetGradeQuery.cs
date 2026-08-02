using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Queries
{
    public class GetGradeQuery : IRequest<GradeDto>
    {
        public Guid GradeId { get; set; }
    }

    public class GetGradeQueryHandler : IRequestHandler<GetGradeQuery, GradeDto>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly ILogger<GetGradeQueryHandler> _logger;

        public GetGradeQueryHandler(IGradeRepository gradeRepository, ILogger<GetGradeQueryHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _logger = logger;
        }

        public async Task<GradeDto> Handle(GetGradeQuery request, CancellationToken cancellationToken)
        {
            var grade = await _gradeRepository.GetByIdAsync(request.GradeId, cancellationToken);
            if (grade == null)
                throw new NotFoundException("Grade", request.GradeId);

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
                Credits = grade.Unit?.Credits ?? 0,
                GradePoints = grade.GradeValue != null ? GetGradePoints(grade.GradeValue) : null
            };
        }

        private static int? GetGradePoints(string? gradeValue)
        {
            return gradeValue switch
            {
                "A" => 12,
                "A-" => 11,
                "B+" => 10,
                "B" => 9,
                "B-" => 8,
                "C+" => 7,
                "C" => 6,
                "C-" => 5,
                "D+" => 4,
                "D" => 3,
                "D-" => 2,
                "E" => 1,
                "F" => 0,
                _ => null
            };
        }
    }
}
