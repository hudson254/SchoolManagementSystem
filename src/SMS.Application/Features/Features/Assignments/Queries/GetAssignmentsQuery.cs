using MediatR;
using SMS.Application.DTOs;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.Assignments.Queries
{
    public class GetAssignmentsQuery : IRequest<PagedResult<AssignmentDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? LecturerId { get; set; }
        public Guid? SemesterId { get; set; }
        public string? Status { get; set; }
        public bool? IsGraded { get; set; }
        public string SortBy { get; set; } = "DueDate";
        public bool SortDescending { get; set; } = false;
    }

    public class GetAssignmentsQueryHandler : IRequestHandler<GetAssignmentsQuery, PagedResult<AssignmentDto>>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<GetAssignmentsQueryHandler> _logger;

        public GetAssignmentsQueryHandler(
            IAssignmentRepository assignmentRepository,
            ILogger<GetAssignmentsQueryHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        public async Task<PagedResult<AssignmentDto>> Handle(GetAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var assignments = await _assignmentRepository.GetAssignmentsAsync(
                request.Page,
                request.PageSize,
                request.SearchTerm,
                request.UnitId,
                request.LecturerId,
                request.SemesterId,
                request.Status,
                request.IsGraded,
                request.SortBy,
                request.SortDescending,
                cancellationToken);

            var totalCount = await _assignmentRepository.CountAssignmentsAsync(
                request.SearchTerm,
                request.UnitId,
                request.LecturerId,
                request.SemesterId,
                request.Status,
                request.IsGraded,
                cancellationToken);

            var dtos = new List<AssignmentDto>();

            foreach (var a in assignments)
            {
                var submissions = await _assignmentRepository.GetSubmissionsAsync(a.Id, cancellationToken);
                var submissionCount = submissions.Count();
                var gradedCount = submissions.Count(s => s.Status == "Graded");

                dtos.Add(new AssignmentDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    UnitId = a.UnitId,
                    LecturerId = a.LecturerId,
                    SemesterId = a.SemesterId,
                    MaxScore = a.MaxScore,
                    Weight = a.Weight,
                    DueDate = a.DueDate,
                    PublishedDate = a.PublishedDate,
                    ClosingDate = a.ClosingDate,
                    Instructions = a.Instructions,
                    Attachments = a.Attachments,
                    Status = a.Status,
                    IsGraded = a.IsGraded,
                    AllowLateSubmission = a.AllowLateSubmission,
                    LatePenaltyPercent = a.LatePenaltyPercent,
                    UnitName = a.Unit?.Name ?? string.Empty,
                    UnitCode = a.Unit?.Code ?? string.Empty,
                    LecturerName = a.Lecturer?.User.FullName ?? string.Empty,
                    SemesterName = a.Semester?.Name ?? string.Empty,
                    SubmissionCount = submissionCount,
                    GradedCount = gradedCount
                });
            }

            return new PagedResult<AssignmentDto>
            {
                Items = dtos,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
    }
}