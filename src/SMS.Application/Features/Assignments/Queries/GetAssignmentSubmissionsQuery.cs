using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Queries
{
    public class GetAssignmentSubmissionsQuery : IRequest<PagedResult<AssignmentSubmissionDto>>
    {
        public Guid AssignmentId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetAssignmentSubmissionsQueryHandler : IRequestHandler<GetAssignmentSubmissionsQuery, PagedResult<AssignmentSubmissionDto>>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<GetAssignmentSubmissionsQueryHandler> _logger;

        public GetAssignmentSubmissionsQueryHandler(
            IAssignmentRepository assignmentRepository,
            ILogger<GetAssignmentSubmissionsQueryHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        public async Task<PagedResult<AssignmentSubmissionDto>> Handle(GetAssignmentSubmissionsQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
                throw new NotFoundException("Assignment", request.AssignmentId);

            var submissions = await _assignmentRepository.GetSubmissionsAsync(request.AssignmentId, cancellationToken);
            var allSubmissions = submissions.ToList();
            var totalCount = allSubmissions.Count;

            var pagedSubmissions = allSubmissions
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var dtos = pagedSubmissions.Select(s => new AssignmentSubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                StudentId = s.StudentId,
                StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : string.Empty,
                StudentNumber = s.Student?.StudentNumber ?? string.Empty,
                AssignmentTitle = s.Assignment?.Title ?? string.Empty,
                MaxScore = s.Assignment?.MaxScore ?? 0,
                SubmissionDate = s.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                FilePath = s.FilePath,
                FileName = s.FileName,
                Comments = s.Comments,
                Score = s.Score ?? 0,
                GradedScore = s.Score,
                Feedback = s.Feedback,
                Status = s.Status ?? "Submitted",
                IsLate = s.IsLate,
                SubmittedAt = s.SubmittedAt,
                GradedAt = s.GradedDate,
                GraderName = s.GraderName,
                GradedDate = s.GradedDate?.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            return new PagedResult<AssignmentSubmissionDto>
            {
                Items = dtos,
                Page = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}
