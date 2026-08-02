using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Grades.Queries
{
    public class GetGradesQuery : IRequest<PagedResult<GradeDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? StudentId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? SemesterId { get; set; }
        public bool? IsPublished { get; set; }
    }

    public class GetGradesQueryHandler : IRequestHandler<GetGradesQuery, PagedResult<GradeDto>>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly ILogger<GetGradesQueryHandler> _logger;

        public GetGradesQueryHandler(IGradeRepository gradeRepository, ILogger<GetGradesQueryHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _logger = logger;
        }

        public async Task<PagedResult<GradeDto>> Handle(GetGradesQuery request, CancellationToken cancellationToken)
        {
            var allGrades = await _gradeRepository.GetAllGradesAsync(cancellationToken);
            var query = allGrades.AsQueryable();

            if (request.StudentId.HasValue)
                query = query.Where(g => g.StudentId == request.StudentId.Value);
            if (request.UnitId.HasValue)
                query = query.Where(g => g.UnitId == request.UnitId.Value);
            if (request.SemesterId.HasValue)
                query = query.Where(g => g.SemesterId == request.SemesterId.Value);
            if (request.IsPublished.HasValue)
                query = query.Where(g => g.IsPublished == request.IsPublished.Value);

            var list = query.ToList();
            var totalCount = list.Count;

            var pagedItems = list
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(g => new GradeDto
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
                })
                .ToList();

            return new PagedResult<GradeDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = request.Page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
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
