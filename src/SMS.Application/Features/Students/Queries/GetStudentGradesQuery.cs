using MediatR;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

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

            var grades = await _gradeRepository.GetStudentGradesAsync(
                request.StudentId,
                request.SemesterId,
                request.IsPublished,
                cancellationToken);

            return grades.Select(g => new GradeDto
            {
                Id = g.Id,
                StudentId = g.StudentId,
                EnrollmentId = g.EnrollmentId,
                GradeValue = g.GradeValue,
                Score = g.Score,
                Remarks = g.Remarks,
                GradedDate = g.GradedDate,
                IsPublished = g.IsPublished,
                PublishedDate = g.PublishedDate,
                StudentName = g.Student.User.FullName,
                StudentNumber = g.Student.StudentNumber,
                UnitName = g.Enrollment.Unit.Name,
                UnitCode = g.Enrollment.Unit.Code,
                Credits = g.Enrollment.Unit.Credits,
                GradePoints = g.GradeValue != null ? 
                    Domain.Common.DomainConstants.GradeValues.GradePoints.GetValueOrDefault(g.GradeValue) : null
            });
        }
    }
}