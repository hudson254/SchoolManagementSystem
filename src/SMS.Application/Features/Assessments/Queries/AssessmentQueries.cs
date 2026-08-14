using MediatR;
using SMS.Application.Features.Assessments.DTOs;

namespace SMS.Application.Features.Assessments.Queries
{
    public class GetAssessmentTypesQuery : IRequest<IEnumerable<AssessmentTypeDto>> { }
    public class GetAssessmentTypeQuery : IRequest<AssessmentTypeDto> { public Guid Id { get; set; } }
    public class GetAssessmentQuery : IRequest<AssessmentDto> { public Guid Id { get; set; } }
    public class GetAssessmentsByUnitQuery : IRequest<IEnumerable<AssessmentDto>> { public Guid UnitId { get; set; } }
    public class ValidateWeightsQuery : IRequest<WeightValidationResult> { public Guid UnitId { get; set; } }
    public class GetStudentResultQuery : IRequest<StudentResultDto> { public Guid UnitId { get; set; } public Guid StudentId { get; set; } }
    public class CalculateResultsQuery : IRequest<IEnumerable<StudentResultDto>> { public Guid UnitId { get; set; } }
    public class GetStudentResultsQuery : IRequest<IEnumerable<StudentResultDto>> { public Guid StudentId { get; set; } }
    public class GetAssessmentAuditLogQuery : IRequest<IEnumerable<AuditLogDto>> { public Guid? UnitId { get; set; } public Guid? AssessmentId { get; set; } }
    public class GetLecturerDashboardQuery : IRequest<LecturerDashboardDto> { public Guid LecturerId { get; set; } }
}

