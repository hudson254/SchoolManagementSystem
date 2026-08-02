using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Queries
{
    public class GetUnitGradesQuery : IRequest<IEnumerable<GradeDto>>
    {
        public Guid UnitId { get; set; }
        public Guid? SemesterId { get; set; }
    }

    public class GetUnitGradesQueryHandler : IRequestHandler<GetUnitGradesQuery, IEnumerable<GradeDto>>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<GetUnitGradesQueryHandler> _logger;

        public GetUnitGradesQueryHandler(
            IGradeRepository gradeRepository,
            IUnitRepository unitRepository,
            ILogger<GetUnitGradesQueryHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _unitRepository = unitRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<GradeDto>> Handle(GetUnitGradesQuery request, CancellationToken cancellationToken)
        {
            var unit = await _unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
            if (unit == null)
                throw new NotFoundException("Unit", request.UnitId);

            var grades = await _gradeRepository.GetGradesByUnitAsync(request.UnitId);

            if (request.SemesterId.HasValue)
                grades = grades.Where(g => g.SemesterId == request.SemesterId.Value);

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
                StudentName = g.Student != null ? $"{g.Student.FirstName} {g.Student.LastName}" : string.Empty,
                StudentNumber = g.Student?.StudentNumber ?? string.Empty,
                UnitName = g.Unit?.Name ?? string.Empty,
                UnitCode = g.Unit?.Code ?? string.Empty,
                Credits = g.Unit?.Credits ?? 0,
                GradePoints = g.GradeValue != null ? GetGradePoints(g.GradeValue) : null
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
