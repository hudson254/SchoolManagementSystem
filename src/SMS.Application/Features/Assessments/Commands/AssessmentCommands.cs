using MediatR;
using SMS.Domain.Enums;
using SMS.Application.Features.Assessments.DTOs;

namespace SMS.Application.Features.Assessments.Commands
{
    public class CreateAssessmentTypeCommand : IRequest<AssessmentTypeDto>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AssessmentTypeCategory Category { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateAssessmentTypeCommand : IRequest<AssessmentTypeDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AssessmentTypeCategory Category { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateAssessmentCommand : IRequest<AssessmentDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid AssessmentTypeId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? LecturerId { get; set; }
        public decimal Weight { get; set; }
        public int MaxMarks { get; set; } = 100;
        public DateTime? DueDate { get; set; }
        public Guid? TemplateId { get; set; }
    }

    public class UpdateAssessmentCommand : IRequest<AssessmentDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public int MaxMarks { get; set; } = 100;
        public DateTime? DueDate { get; set; }
    }

    public class EnterMarkCommand : IRequest<StudentAssessmentMarkDto>
    {
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public string? Feedback { get; set; }
        public bool IsDraft { get; set; }
        public string? Reason { get; set; }
    }

    public class ImportMarksCommand : IRequest<BulkMarkImportResult>
    {
        public Guid AssessmentId { get; set; }
        public List<MarkImportRecord> Records { get; set; } = new();
        public Guid? ImportBatchId { get; set; }
    }

    public class MarkImportRecord
    {
        public Guid StudentId { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public string? Feedback { get; set; }
    }

    public class UpdateMarkCommand : IRequest<StudentAssessmentMarkDto>
    {
        public Guid MarkId { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public string? Feedback { get; set; }
        public bool IsDraft { get; set; }
        public string? Reason { get; set; }
    }

    public class SubmitForReviewCommand : IRequest
    {
        public Guid UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public string? Comments { get; set; }
    }

    public class ApproveResultsCommand : IRequest
    {
        public Guid UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public string? Comments { get; set; }
    }

    public class PublishResultsCommand : IRequest
    {
        public Guid UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public string? Comments { get; set; }
    }

    public class ChangeMarkCommand : IRequest
    {
        public Guid MarkId { get; set; }
        public decimal NewScore { get; set; }
        public decimal NewMaxScore { get; set; }
        public string? Reason { get; set; }
    }

    public class LockUnitCommand : IRequest
    {
        public Guid UnitId { get; set; }
        public string? Reason { get; set; }
    }

    public class UnlockUnitCommand : IRequest
    {
        public Guid UnitId { get; set; }
        public string? Reason { get; set; }
    }
}

