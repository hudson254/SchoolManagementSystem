using FluentValidation;
using SMS.Shared.DTOs;
using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;

namespace SMS.Application.Features.Dashboard.Queries
{
    public class GetTopStudentsQuery : IRequest<IEnumerable<TopStudentDto>>
    {
        public int Count { get; set; } = 10;
        public Guid? SemesterId { get; set; }
    }

    /// <summary>
    /// Top-students dashboard feed backed by the authoritative grading engine
    /// (published UnitResults). Grade points come from the grading-scale snapshot
    /// persisted on each result, never from hard-coded point dictionaries.
    /// </summary>
    public class GetTopStudentsQueryHandler : IRequestHandler<GetTopStudentsQuery, IEnumerable<TopStudentDto>>
    {
        private readonly IUnitResultRepository _unitResultRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<GetTopStudentsQueryHandler> _logger;

        public GetTopStudentsQueryHandler(
            IUnitResultRepository unitResultRepository,
            IStudentRepository studentRepository,
            ILogger<GetTopStudentsQueryHandler> logger)
        {
            _unitResultRepository = unitResultRepository;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<TopStudentDto>> Handle(GetTopStudentsQuery request, CancellationToken cancellationToken)
        {
            var results = (await _unitResultRepository.GetAllWithDetailsAsync(cancellationToken))
                .Where(r => !r.IsDeleted && r.IsPublished)
                .ToList();

            if (request.SemesterId.HasValue)
                results = results.Where(r => r.SemesterId == request.SemesterId.Value).ToList();

            var studentGrades = results
                .Where(r => !string.IsNullOrWhiteSpace(r.GradeLetter))
                .GroupBy(r => r.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    GPA = g.Average(x => x.GpaPoints ?? 0m),
                    CreditsEarned = g.Sum(x => x.Unit?.Credits ?? 0)
                })
                .OrderByDescending(x => x.GPA)
                .ThenByDescending(x => x.CreditsEarned)
                .Take(request.Count)
                .ToList();

            var result = new List<TopStudentDto>();

            foreach (var s in studentGrades)
            {
                var student = await _studentRepository.GetStudentWithDetailsAsync(s.StudentId, cancellationToken);
                if (student != null)
                {
                    result.Add(new TopStudentDto
                    {
                        StudentId = student.Id,
                        StudentName = student.User.FullName,
                        StudentNumber = student.StudentNumber,
                        ProgrammeName = student.Programme?.Name ?? "Not Enrolled",
                        GPA = (decimal)s.GPA,
                        CreditsEarned = s.CreditsEarned
                    });
                }
            }

            return result;
        }
    }
}