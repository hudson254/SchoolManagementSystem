using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.Features.Enrollments.Commands;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Application.Features.Assignments.Commands;
using SMS.Application.Features.OMS.Queries;
using SMS.Application.Features.OMS.Commands;
using SMS.Application.Features.OMS.Dtos;
using SMS.Application.Features.OMS.Queries;
using SMS.Domain.Enums;

namespace SMS.API.Controllers.v1.Oms;

[ApiVersion("1.0")]
[Authorize]
[Tags("OMS-Requests")]
[Route("api/v{version:apiVersion}/oms/requests")]
public class OmsRequestsController : BaseApiController
{
    [HttpGet]
    [Authorize(Policy = OmsPolicy.CanViewOwnRequest)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] RequestStatus? status = null,
        [FromQuery] string? requestType = null,
        [FromQuery] string? requesterUserId = null,
        [FromQuery] string? assignedUserId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? scope = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetRequestsQuery
        {
            Status = status,
            RequestType = requestType,
            RequesterUserId = requesterUserId,
            AssignedUserId = assignedUserId,
            Search = search,
            Scope = scope,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{requestId:guid}")]
    [Authorize(Policy = OmsPolicy.CanViewOwnRequest)]
    public async Task<IActionResult> GetRequestById(Guid requestId, CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new GetRequestByIdQuery { RequestId = requestId }, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = OmsPolicy.CanCreateRequest)]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRequestById), new { requestId = result.Id }, result);
    }

    // ---------------------------------------------------------------------
    // Thin module adapters (Phase 3).
    //
    // These endpoints ONLY create a generic OMS Request pre-populated with
    // module business context. They deliberately expose no approve/reject/
    // assign verbs: the entire lifecycle is served by the generic endpoints
    // above, so there is exactly one workflow engine.
    //
    // The response is the ordinary generic Request DTO, and the returned id
    // can be used with every generic request endpoint from here on.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Creates an OMS request against an existing enrollment (exception, unit
    /// drop or course change). The Enrollment module remains authoritative for
    /// enrollment state; this only raises a request.
    /// </summary>
    [HttpPost("enrollment")]
    [Authorize(Policy = OmsPolicy.CanCreateRequest)]
    public async Task<IActionResult> CreateEnrollmentRequest(
        [FromBody] CreateEnrollmentRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRequestById), new { requestId = result.Id }, result);
    }

    [HttpPost("accommodation")]
    [Authorize(Policy = OmsPolicy.CanCreateRequest)]
    public async Task<IActionResult> CreateAccommodationRequest(
        [FromBody] CreateAccommodationRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRequestById), new { requestId = result.Id }, result);
    }

    [HttpPost("assignment")]
    [Authorize(Policy = OmsPolicy.CanCreateRequest)]
    public async Task<IActionResult> CreateAssignmentRequest(
        [FromBody] CreateAssignmentRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRequestById), new { requestId = result.Id }, result);
    }

    [HttpPut("{requestId:guid}")]
    [Authorize(Policy = OmsPolicy.CanUpdateRequest)]
    public async Task<IActionResult> UpdateRequest(
        Guid requestId,
        [FromBody] UpdateRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new UpdateRequestCommand
        {
            RequestId = requestId,
            Title = command.Title,
            Description = command.Description,
            Priority = command.Priority,
            DueDate = command.DueDate,
            RowVersion = command.RowVersion
        }, cancellationToken));
    }

    [HttpPost("{requestId:guid}/submit")]
    [Authorize(Policy = OmsPolicy.CanSubmitRequest)]
    public async Task<IActionResult> SubmitRequest(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new SubmitRequestCommand { RequestId = requestId }, cancellationToken));
    }

    [HttpPost("{requestId:guid}/assign")]
    [Authorize(Policy = OmsPolicy.CanAssignRequest)]
    public async Task<IActionResult> AssignRequest(
        Guid requestId,
        [FromBody] AssignRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/reassign")]
    [Authorize(Policy = OmsPolicy.CanReassignRequest)]
    public async Task<IActionResult> ReassignRequest(
        Guid requestId,
        [FromBody] ReassignRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/start-review")]
    [Authorize(Policy = OmsPolicy.CanReviewRequest)]
    public async Task<IActionResult> StartReview(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new StartRequestReviewCommand { RequestId = requestId }, cancellationToken));
    }

    [HttpPost("{requestId:guid}/approve")]
    [Authorize(Policy = OmsPolicy.CanApproveRequest)]
    public async Task<IActionResult> ApproveRequest(
        Guid requestId,
        [FromBody] ApproveRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/reject")]
    [Authorize(Policy = OmsPolicy.CanRejectRequest)]
    public async Task<IActionResult> RejectRequest(
        Guid requestId,
        [FromBody] RejectRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/return-for-correction")]
    [Authorize(Policy = OmsPolicy.CanReturnRequest)]
    public async Task<IActionResult> ReturnForCorrection(
        Guid requestId,
        [FromBody] ReturnRequestForCorrectionCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/cancel")]
    [Authorize(Policy = OmsPolicy.CanCancelOwnRequest)]
    public async Task<IActionResult> CancelRequest(
        Guid requestId,
        [FromBody] CancelRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/complete")]
    [Authorize(Policy = OmsPolicy.CanCompleteRequest)]
    public async Task<IActionResult> CompleteRequest(
        Guid requestId,
        [FromBody] CompleteRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/escalate")]
    [Authorize(Policy = OmsPolicy.CanEscalateRequest)]
    public async Task<IActionResult> EscalateRequest(
        Guid requestId,
        [FromBody] EscalateRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{requestId:guid}/comments")]
    [Authorize(Policy = OmsPolicy.CanCommentOnRequest)]
    public async Task<IActionResult> AddComment(
        Guid requestId,
        [FromBody] AddRequestCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        command.RequestId = requestId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpGet("{requestId:guid}/history")]
    [Authorize(Policy = OmsPolicy.CanViewOwnRequest)]
    public async Task<IActionResult> GetRequestHistory(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(
            new GetRequestStatusHistoryQuery { RequestId = requestId }, cancellationToken));
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = OmsPolicy.CanViewRequests)]
    public async Task<IActionResult> GetDashboardSummary(
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new GetRequestDashboardSummaryQuery(), cancellationToken));
    }

    [HttpGet("types")]
    [Authorize(Policy = OmsPolicy.CanViewOwnRequest)]
    public async Task<IActionResult> GetRequestTypes(
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new GetRequestTypesQuery(), cancellationToken));
    }

    [HttpPost("types")]
    [Authorize(Policy = OmsPolicy.CanManageRequestTypes)]
    public async Task<IActionResult> CreateRequestType(
        [FromBody] CreateRequestTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRequestTypes), null, result);
    }

    [HttpPut("types/{requestTypeId:guid}")]
    [Authorize(Policy = OmsPolicy.CanManageRequestTypes)]
    public async Task<IActionResult> UpdateRequestType(
        Guid requestTypeId,
        [FromBody] UpdateRequestTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new UpdateRequestTypeCommand
        {
            RequestTypeId = requestTypeId,
            DisplayName = command.DisplayName,
            Description = command.Description,
            IsActive = command.IsActive
        }, cancellationToken));
    }

    [HttpGet("types/{requestTypeId:guid}")]
    [Authorize(Policy = OmsPolicy.CanViewRequests)]
    public async Task<IActionResult> GetRequestTypeById(
        Guid requestTypeId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new GetRequestTypeByIdQuery { Id = requestTypeId }, cancellationToken));
    }

    [HttpPost("{requestId:guid}/attachments")]
    [Authorize(Policy = OmsPolicy.CanUpdateRequest)]
    [RequestSizeLimit(RequestAttachmentPolicy.MaxFileSizeBytes)]
    public async Task<IActionResult> UploadRequestAttachment(
        Guid requestId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "A file is required." });

        await using var stream = file.OpenReadStream();
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var result = await Mediator.Send(new UploadRequestAttachmentCommand
        {
            RequestId = requestId,
            FileStream = buffer,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        }, cancellationToken);

        return CreatedAtAction(nameof(GetRequestAttachments), new { requestId }, result);
    }

    [HttpGet("{requestId:guid}/attachments")]
    [Authorize(Policy = OmsPolicy.CanViewOwnRequest)]
    public async Task<IActionResult> GetRequestAttachments(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(
            new GetRequestAttachmentsQuery { RequestId = requestId }, cancellationToken));
    }

    [HttpGet("attachments/{attachmentId:guid}/download")]
    [Authorize(Policy = OmsPolicy.CanViewOwnRequest)]
    public async Task<IActionResult> DownloadRequestAttachment(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            new DownloadRequestAttachmentQuery { AttachmentId = attachmentId }, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpDelete("attachments/{attachmentId:guid}")]
    [Authorize(Policy = OmsPolicy.CanUpdateRequest)]
    public async Task<IActionResult> DeleteRequestAttachment(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(
            new DeleteRequestAttachmentCommand { AttachmentId = attachmentId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("types/{requestTypeId:guid}/activate")]
    [Authorize(Policy = OmsPolicy.CanManageRequestTypes)]
    public async Task<IActionResult> ActivateRequestType(
        Guid requestTypeId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Mediator.Send(new ActivateRequestTypeCommand { Id = requestTypeId }, cancellationToken));
    }

    [HttpDelete("types/{requestTypeId:guid}")]
    [Authorize(Policy = OmsPolicy.CanManageRequestTypes)]
    public async Task<IActionResult> DeactivateRequestType(
        Guid requestTypeId,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new DeactivateRequestTypeCommand { RequestTypeId = requestTypeId }, cancellationToken);
        return NoContent();
    }
}

