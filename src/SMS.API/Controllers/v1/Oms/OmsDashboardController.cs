using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.Features.OMS.Dtos;
using SMS.Application.Features.OMS.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace SMS.API.Controllers.v1.Oms;

[ApiVersion("1.0")]
[Authorize]
[Tags("OMS")]
[Route("api/v{version:apiVersion}/oms/dashboard")]
public class OmsDashboardController : BaseApiController
{
    private readonly ILogger<OmsDashboardController> _logger;

    public OmsDashboardController(ILogger<OmsDashboardController> logger)
    {
        _logger = logger;
    }

    [HttpGet("summary")]
    [Authorize(Policy = OmsPolicy.CanViewOrders)]
    [SwaggerOperation(Summary = "OMS dashboard summary")]
    [SwaggerResponse(StatusCodes.Status200OK, "Dashboard summary", Type = typeof(OmsDashboardSummaryDto))]
    [ProducesResponseType(typeof(OmsDashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = new OmsDashboardSummaryQuery
        {
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
