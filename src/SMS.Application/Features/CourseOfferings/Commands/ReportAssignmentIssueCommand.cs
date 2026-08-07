using System;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SMS.Application.DTOs;
using SMS.Application.Exceptions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.CourseOfferings.Commands
{
    public class ReportAssignmentIssueCommand : IRequest<AssignmentIssueReportDto>
    {
        public Guid ReporterUserId { get; set; }
        public string AssignmentType { get; set; } = string.Empty; // Enrollment / Teaching
        public Guid CourseOfferingId { get; set; }
        public Guid? CourseOfferingEnrollmentId { get; set; }
        public Guid? CourseOfferingLecturerId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ReportAssignmentIssueCommandValidator : AbstractValidator<ReportAssignmentIssueCommand>
    {
        public ReportAssignmentIssueCommandValidator()
        {
            RuleFor(x => x.ReporterUserId)
                .NotEmpty().WithMessage("Reporter user ID is required");
            RuleFor(x => x.AssignmentType)
                .NotEmpty().WithMessage("Assignment type is required")
                .Must(t => t == "Enrollment" || t == "Teaching")
                .WithMessage("Assignment type must be 'Enrollment' or 'Teaching'");
            RuleFor(x => x.CourseOfferingId)
                .NotEmpty().WithMessage("Course offering ID is required");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters");
        }
    }

    public class ReportAssignmentIssueCommandHandler
        : IRequestHandler<ReportAssignmentIssueCommand, AssignmentIssueReportDto>
    {
        private readonly IAssignmentIssueReportRepository _issueRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;
        private readonly ILogger<ReportAssignmentIssueCommandHandler> _logger;

        public ReportAssignmentIssueCommandHandler(
            IAssignmentIssueReportRepository issueRepository,
            IUnitOfWork unitOfWork,
            IAuditService auditService,
            ILogger<ReportAssignmentIssueCommandHandler> logger)
        {
            _issueRepository = issueRepository;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AssignmentIssueReportDto> Handle(
            ReportAssignmentIssueCommand request,
            CancellationToken cancellationToken)
        {
            var issue = new AssignmentIssueReport
            {
                ReporterUserId = request.ReporterUserId,
                AssignmentType = request.AssignmentType,
                CourseOfferingId = request.CourseOfferingId,
                CourseOfferingEnrollmentId = request.CourseOfferingEnrollmentId,
                CourseOfferingLecturerId = request.CourseOfferingLecturerId,
                Reason = request.Reason,
                Status = AssignmentIssueStatus.Pending
            };

            await _issueRepository.AddAsync(issue, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("AssignmentIssueReport", "Report",
                issue.Id.ToString());

            _logger.LogInformation("Issue reported by user {ReporterUserId} for offering {CourseOfferingId} (type: {AssignmentType})",
                request.ReporterUserId, request.CourseOfferingId, request.AssignmentType);

            return new AssignmentIssueReportDto
            {
                Id = issue.Id,
                CourseOfferingId = issue.CourseOfferingId,
                IssueType = issue.AssignmentType,
                Description = issue.Reason,
                Status = issue.Status,
                ReportedDate = issue.CreatedAt
            };
        }
    }
}
