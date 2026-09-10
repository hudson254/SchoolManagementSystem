using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Semesters.Queries
{
    public class GetSemestersQuery : IRequest<IEnumerable<SemesterDto>>
    {
        public bool IncludeInactive { get; set; }
    }

    public class GetSemestersQueryHandler : IRequestHandler<GetSemestersQuery, IEnumerable<SemesterDto>>
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly ILogger<GetSemestersQueryHandler> _logger;

        public GetSemestersQueryHandler(ISemesterRepository semesterRepository, ILogger<GetSemestersQueryHandler> logger)
        {
            _semesterRepository = semesterRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<SemesterDto>> Handle(GetSemestersQuery request, CancellationToken cancellationToken)
        {
            var semesters = await _semesterRepository.GetAllAsync(cancellationToken);

            var result = semesters
                .Where(s => request.IncludeInactive || s.IsActive)
                .Select(s => new SemesterDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.SemesterNumber.ToString(),
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    IsActive = s.IsActive
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} semesters", result.Count);
            return result;
        }
    }
}