using MediatR;
using SMS.Application.DTOs;

// Re-export from Students.Queries to maintain GradeController compatibility
namespace SMS.Application.Features.Grades.Queries
{
    public class GetStudentGradesQuery : IRequest<IEnumerable<GradeDto>>
    {
        public System.Guid StudentId { get; set; }
        public System.Guid? SemesterId { get; set; }
    }

    public class GetStudentGradesHandler : IRequestHandler<GetStudentGradesQuery, IEnumerable<GradeDto>>
    {
        private readonly IRequestHandler<SMS.Application.Features.Students.Queries.GetStudentGradesQuery, IEnumerable<GradeDto>> _innerHandler;

        public GetStudentGradesHandler(
            IRequestHandler<SMS.Application.Features.Students.Queries.GetStudentGradesQuery, IEnumerable<GradeDto>> innerHandler)
        {
            _innerHandler = innerHandler;
        }

        public async System.Threading.Tasks.Task<IEnumerable<GradeDto>> Handle(GetStudentGradesQuery request, System.Threading.CancellationToken cancellationToken)
        {
            var innerQuery = new SMS.Application.Features.Students.Queries.GetStudentGradesQuery
            {
                StudentId = request.StudentId,
                SemesterId = request.SemesterId
            };
            return await _innerHandler.Handle(innerQuery, cancellationToken);
        }
    }
}

