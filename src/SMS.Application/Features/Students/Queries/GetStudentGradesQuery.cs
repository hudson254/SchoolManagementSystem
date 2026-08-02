using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Multitenancy.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Students.Queries
{
    public class GetStudentGradesQuery : IRequest<IEnumerable<GradeDto>>
    {
        public Guid StudentId { get; set; }
        public Guid? SemesterId { get; set; }
        public bool? IsPublished { get; set; }
    }

    public class GetStudentGradesQueryHandler : IRequestHandler<GetStudentGradesQuery, IEnumerable<GradeDto>>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IGradeRepository _gradeRepository;
        private readonly ILogger<GetStudentGradesQueryHandler> _logger;

        public GetStudentGradesQueryHandler(
            IStudentRepository studentRepository,
            IGradeRepository gradeRepository,
            ILogger<GetStudentGradesQueryHandler> logger)
        {
            _studentRepository = studentRepository;
            _gradeRepository = gradeRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<GradeDto>> Handle(GetStudentGradesQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student == null)
            {
                throw new NotFoundException("Student", request.StudentId);
            }

            var grades = await _gradeRepository.GetStudentGradesAsync(request.StudentId);

            return grades.Select(g => new GradeDto
            {
                Id = g.Id,
                StudentId = g.StudentId,
                EnrollmentId = g.EnrollmentId ?? Guid.Empty,
                GradeValue = g.GradeValue,
                Score = g.Score,
                Remarks = g.Remarks,
                GradedDate = g.GradedDate,
                IsPublished = g.IsPublished,
                PublishedDate = g.PublishedDate,
                StudentName = g.Student?.User?.FullName ?? "",
                StudentNumber = g.Student?.StudentNumber ?? "",
                UnitName = g.Enrollment?.Unit?.Name ?? g.Unit?.Name ?? "",
                UnitCode = g.Unit?.Code ?? "",
                Credits = g.Unit?.Credits ?? 0,
                GradePoints = g.GradeValue != null ?
                                GetGradePoints(g.GradeValue) : null
            });
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




