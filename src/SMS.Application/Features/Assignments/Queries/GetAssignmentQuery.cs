using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Assignments.Queries
{
    public class GetAssignmentQuery : IRequest<AssignmentDto>
    {
        public Guid AssignmentId { get; set; }
    }

    public class GetAssignmentQueryHandler : IRequestHandler<GetAssignmentQuery, AssignmentDto>
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILogger<GetAssignmentQueryHandler> _logger;

        public GetAssignmentQueryHandler(
            IAssignmentRepository assignmentRepository,
            ILogger<GetAssignmentQueryHandler> logger)
        {
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        public async Task<AssignmentDto> Handle(GetAssignmentQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _assignmentRepository.GetAssignmentWithDetailsAsync(request.AssignmentId, cancellationToken);

            if (assignment == null)
            {
                throw new NotFoundException("Assignment", request.AssignmentId);
            }

            var submissions = await _assignmentRepository.GetSubmissionsAsync(assignment.Id, cancellationToken);
            var submissionCount = submissions.Count();
            var gradedCount = submissions.Count(s => s.Status == "Graded");

            return new AssignmentDto
            {
                Id = assignment.Id,
                Title = assignment.Title,
                Description = assignment.Description,
                UnitId = assignment.UnitId,
                LecturerId = assignment.LecturerId,
                SemesterId = assignment.SemesterId,
                MaxScore = assignment.MaxScore,
                Weight = assignment.Weight,
                DueDate = assignment.DueDate,
                PublishedDate = assignment.PublishedDate,
                ClosingDate = assignment.ClosingDate,
                Instructions = assignment.Instructions,
                Attachments = assignment.Attachments,
                Status = assignment.Status,
                IsGraded = assignment.IsGraded,
                AllowLateSubmission = assignment.AllowLateSubmission,
                LatePenaltyPercent = assignment.LatePenaltyPercent,
                UnitName = assignment.Unit?.Name ?? string.Empty,
                UnitCode = assignment.Unit?.Code ?? string.Empty,
                LecturerName = assignment.Lecturer?.User.FullName ?? string.Empty,
                SemesterName = assignment.Semester?.Name ?? string.Empty,
                SubmissionCount = submissionCount,
                GradedCount = gradedCount
            };
        }
    }
}




