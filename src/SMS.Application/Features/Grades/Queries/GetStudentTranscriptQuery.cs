using MediatR;
using SMS.Application.DTOs;

// Re-export from Students.Queries to maintain GradeController compatibility
namespace SMS.Application.Features.Grades.Queries
{
    public class GetStudentTranscriptQuery : IRequest<TranscriptDto>
    {
        public System.Guid StudentId { get; set; }
    }

    public class GetStudentTranscriptHandler : IRequestHandler<GetStudentTranscriptQuery, TranscriptDto>
    {
        private readonly IRequestHandler<SMS.Application.Features.Students.Queries.GetStudentTranscriptQuery, TranscriptDto> _innerHandler;

        public GetStudentTranscriptHandler(
            IRequestHandler<SMS.Application.Features.Students.Queries.GetStudentTranscriptQuery, TranscriptDto> innerHandler)
        {
            _innerHandler = innerHandler;
        }

        public async System.Threading.Tasks.Task<TranscriptDto> Handle(GetStudentTranscriptQuery request, System.Threading.CancellationToken cancellationToken)
        {
            var innerQuery = new SMS.Application.Features.Students.Queries.GetStudentTranscriptQuery
            {
                StudentId = request.StudentId
            };
            return await _innerHandler.Handle(innerQuery, cancellationToken);
        }
    }
}

