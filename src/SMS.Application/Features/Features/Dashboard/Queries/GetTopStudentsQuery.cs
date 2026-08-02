using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Dashboard.Queries
{
    public class GetTopStudentsQuery : IRequest<IEnumerable<TopStudentDto>>
    {
        public int Count { get; set; } = 10;
        public Guid? SemesterId { get; set; }
    }

    public class GetTopStudentsQueryHandler : IRequestHandler<GetTopStudentsQuery, IEnumerable<TopStudentDto>>
    {
        private readonly IGradeRepository _gradeRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<GetTopStudentsQueryHandler> _logger;

        public GetTopStudentsQueryHandler(
            IGradeRepository gradeRepository,
            IStudentRepository studentRepository,
            ILogger<GetTopStudentsQueryHandler> logger)
        {
            _gradeRepository = gradeRepository;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<TopStudentDto>> Handle(GetTopStudentsQuery request, CancellationToken cancellationToken)
        {
            var grades = await _gradeRepository.GetGradesForSemesterAsync(request.SemesterId, cancellationToken);

            var studentGrades = grades
                .Where(g => g.GradeValue != null)
                .GroupBy(g => g.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    GPA = g.Average(x => Domain.Common.DomainConstants.GradeValues.GradePoints.GetValueOrDefault(x.GradeValue, 0)),
                    CreditsEarned = g.Sum(x => x.Enrollment.Unit.Credits)
                })
                .OrderByDescending(x => x.GPA)
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
                        GPA = s.GPA,
                        CreditsEarned = s.CreditsEarned
                    });
                }
            }

            return result;
        }
    }
}