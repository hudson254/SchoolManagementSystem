using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Assessments.Commands;
using SMS.Application.Features.Assessments.Queries;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.GradingScales.Commands;
using SMS.Application.Features.GradingScales.Queries;
using SMS.Application.Features.GradingScales.DTOs;
using SMS.Application.Features.CertificateEligibility.Queries;
using SMS.Application.Features.CertificateEligibility.DTOs;
using SMS.Application.Features.Reporting.Queries;
using SMS.Application.Features.Reporting.DTOs;
using SMS.Application.Features.Moderation.Commands;
using SMS.Application.Features.Moderation.Queries;
using SMS.Application.Features.Moderation.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.API.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class AssessmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AssessmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Phase 3: Assessment Types
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<AssessmentTypeDto>>> GetAssessmentTypes()
        {
            var result = await _mediator.Send(new GetAssessmentTypesQuery());
            return Ok(result);
        }

        [HttpPost("types")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<AssessmentTypeDto>> CreateAssessmentType([FromBody] CreateAssessmentTypeCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAssessmentType), new { id = result.Id }, result);
        }

        [HttpGet("types/{id:guid}")]
        public async Task<ActionResult<AssessmentTypeDto>> GetAssessmentType(Guid id)
        {
            var result = await _mediator.Send(new GetAssessmentTypeQuery { Id = id });
            return Ok(result);
        }

        // Phase 4: Assessment Weight Configuration
        [HttpPost]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult<AssessmentDto>> CreateAssessment([FromBody] CreateAssessmentCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAssessment), new { id = result.Id }, result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AssessmentDto>> GetAssessment(Guid id)
        {
            var result = await _mediator.Send(new GetAssessmentQuery { Id = id });
            return Ok(result);
        }

        [HttpGet("unit/{unitId:guid}")]
        public async Task<ActionResult<IEnumerable<AssessmentDto>>> GetAssessmentsByUnit(Guid unitId)
        {
            var result = await _mediator.Send(new GetAssessmentsByUnitQuery { UnitId = unitId });
            return Ok(result);
        }

        [HttpGet("unit/{unitId:guid}/weights")]
        public async Task<ActionResult<WeightValidationResult>> ValidateWeights(Guid unitId)
        {
            var result = await _mediator.Send(new ValidateWeightsQuery { UnitId = unitId });
            return Ok(result);
        }

        // Phase 5: Manual Mark Entry
        [HttpPost("marks")]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult<StudentAssessmentMarkDto>> EnterMark([FromBody] EnterMarkCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("marks/bulk")]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult<BulkMarkImportResult>> ImportMarks([FromBody] ImportMarksCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("marks/{markId:guid}")]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult<StudentAssessmentMarkDto>> UpdateMark(Guid markId, [FromBody] UpdateMarkCommand command)
        {
            command.MarkId = markId;
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // Phase 10: Result Publication Workflow
        [HttpPost("results/{unitId:guid}/submit")]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult> SubmitForReview(Guid unitId, [FromBody] SubmitForReviewCommand command)
        {
            command.UnitId = unitId;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("results/{unitId:guid}/approve")]
        [Authorize(Roles = "Administrator,Coordinator")]
        public async Task<ActionResult> ApproveResults(Guid unitId, [FromBody] ApproveResultsCommand command)
        {
            command.UnitId = unitId;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("results/{unitId:guid}/publish")]
        [Authorize(Roles = "Administrator,Coordinator")]
        public async Task<ActionResult> PublishResults(Guid unitId, [FromBody] PublishResultsCommand command)
        {
            command.UnitId = unitId;
            await _mediator.Send(command);
            return Ok();
        }

        // Phase 7: Automatic Final Score Calculation
        [HttpGet("results/{unitId:guid}/student/{studentId:guid}")]
        public async Task<ActionResult<StudentResultDto>> GetStudentResult(Guid unitId, Guid studentId)
        {
            var result = await _mediator.Send(new GetStudentResultQuery { UnitId = unitId, StudentId = studentId });
            return Ok(result);
        }

        [HttpPost("results/{unitId:guid}/calculate")]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult<IEnumerable<StudentResultDto>>> CalculateResults(Guid unitId)
        {
            var result = await _mediator.Send(new CalculateResultsQuery { UnitId = unitId });
            return Ok(result);
        }

        // Phase 8: Grading Scale Management
        [HttpGet("grading-scales")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<GradingScaleDto>>> GetGradingScales()
        {
            var result = await _mediator.Send(new GetGradingScalesQuery());
            return Ok(result);
        }

        [HttpPost("grading-scales")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<GradingScaleDto>> CreateGradingScale([FromBody] CreateGradingScaleCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetGradingScale), new { id = result.Id }, result);
        }

        [HttpGet("grading-scales/{id:guid}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<GradingScaleDto>> GetGradingScale(Guid id)
        {
            var result = await _mediator.Send(new GetGradingScaleQuery { Id = id });
            return Ok(result);
        }

        // Phase 9: Certificate Eligibility
        [HttpGet("certificate-eligibility/student/{studentId:guid}")]
        public async Task<ActionResult<StudentCertificateEligibilityDto>> GetCertificateEligibility(Guid studentId)
        {
            var result = await _mediator.Send(new GetStudentCertificateEligibilityQuery { StudentId = studentId });
            return Ok(result);
        }

        // Phase 11: Moderation Workflow
        [HttpPost("moderation/review")]
        [Authorize(Roles = "Coordinator,Administrator")]
        public async Task<ActionResult> ReviewMarks([FromBody] ReviewMarksCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("moderation/approve")]
        [Authorize(Roles = "Coordinator,Administrator")]
        public async Task<ActionResult> ApproveMarks([FromBody] ApproveMarksCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpGet("moderation/pending")]
        [Authorize(Roles = "Coordinator,Administrator")]
        public async Task<ActionResult<IEnumerable<ModerationRecordDto>>> GetPendingModeration()
        {
            var result = await _mediator.Send(new GetPendingModerationQuery());
            return Ok(result);
        }

        // Phase 12: Grade Changes
        [HttpPost("marks/{markId:guid}/change")]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult> ChangeMarkAfterPublication(Guid markId, [FromBody] ChangeMarkCommand command)
        {
            command.MarkId = markId;
            await _mediator.Send(command);
            return Ok();
        }

        // Phase 14: Lecturer Dashboard
        [HttpGet("dashboard/lecturer/{lecturerId:guid}")]
        [Authorize(Roles = "Lecturer,Administrator,Coordinator")]
        public async Task<ActionResult<LecturerDashboardDto>> GetLecturerDashboard(Guid lecturerId)
        {
            var result = await _mediator.Send(new GetLecturerDashboardQuery { LecturerId = lecturerId });
            return Ok(result);
        }

        // Phase 13: Student Portal
        [HttpGet("student/{studentId:guid}/results")]
        public async Task<ActionResult<IEnumerable<StudentResultDto>>> GetStudentResults(Guid studentId)
        {
            var result = await _mediator.Send(new GetStudentResultsQuery { StudentId = studentId });
            return Ok(result);
        }

        // Phase 15: Administrative Controls
        [HttpGet("audit-log")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAssessmentAuditLog([FromQuery] Guid? unitId = null, [FromQuery] Guid? assessmentId = null)
        {
            var result = await _mediator.Send(new GetAssessmentAuditLogQuery { UnitId = unitId, AssessmentId = assessmentId });
            return Ok(result);
        }

        // Phase 18: Reporting
        [HttpGet("reports/grade-distribution/{unitId:guid}")]
        [Authorize(Roles = "Administrator,Coordinator,Lecturer")]
        public async Task<ActionResult<SMS.Application.Features.Reporting.DTOs.GradeDistributionReportDto>> GetGradeDistribution(Guid unitId)
        {
            var result = await _mediator.Send(new GetGradeDistributionQuery { UnitId = unitId });
            return Ok(result);
        }

        [HttpGet("reports/pass-fail-rates/{unitId:guid}")]
        [Authorize(Roles = "Administrator,Coordinator,Lecturer")]
        public async Task<ActionResult<PassFailRateReportDto>> GetPassFailRates(Guid unitId)
        {
            var result = await _mediator.Send(new GetPassFailRateQuery { UnitId = unitId });
            return Ok(result);
        }

        [HttpGet("reports/assessment-summary/{unitId:guid}")]
        [Authorize(Roles = "Administrator,Coordinator,Lecturer")]
        public async Task<ActionResult<AssessmentSummaryReportDto>> GetAssessmentSummary(Guid unitId)
        {
            var result = await _mediator.Send(new GetAssessmentSummaryQuery { UnitId = unitId });
            return Ok(result);
        }

        // Phase 3: Admin-managed assessment types
        [HttpPut("types/{id:guid}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<AssessmentTypeDto>> UpdateAssessmentType(Guid id, [FromBody] UpdateAssessmentTypeCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // Phase 15: Lock/unlock academic periods
        [HttpPost("units/{unitId:guid}/lock")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> LockUnit(Guid unitId, [FromBody] LockUnitCommand command)
        {
            command.UnitId = unitId;
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("units/{unitId:guid}/unlock")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> UnlockUnit(Guid unitId, [FromBody] UnlockUnitCommand command)
        {
            command.UnitId = unitId;
            await _mediator.Send(command);
            return Ok();
        }
    }
}

