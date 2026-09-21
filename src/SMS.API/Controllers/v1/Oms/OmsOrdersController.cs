using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.Features.OMS.Commands;
using SMS.Application.Features.OMS.Dtos;
using SMS.Application.Features.OMS.Queries;
using SMS.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace SMS.API.Controllers.v1.Oms;

/// <summary>
/// OMS order lifecycle endpoints (Phase 2C). Thin HTTP surface: every action
/// delegates to the Application layer via MediatR. Coarse-grained HTTP
/// authorization mirrors SMS.Application.Common.OmsAuthorization; fine-grained
/// rules (creator-only cancellation, ownership, tenant isolation, Draft-only
/// edits) are enforced inside the Application handlers.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[Tags("OMS")]
[Route("api/v{version:apiVersion}/oms/orders")]
public class OmsOrdersController : BaseApiController
{
    /// <summary>Paginated, filterable order list for the current tenant.</summary>
    [HttpGet]
    [Authorize(Policy = OmsPolicy.CanViewOrders)]
    [SwaggerOperation(Summary = "List OMS orders (paginated)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Paged orders", Type = typeof(PagedResult<OrderDto>))]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] string? requestedByUserId = null,
        [FromQuery] Guid? roadAccountId = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetOrdersQuery
        {
            Status = status,
            RequestedByUserId = requestedByUserId,
            RoadAccountId = roadAccountId,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves a single order (with line items) by id.</summary>
    [HttpGet("{orderId:guid}")]
    [Authorize(Policy = OmsPolicy.CanViewOrders)]
    [SwaggerOperation(Summary = "Get an OMS order by id")]
    [SwaggerResponse(StatusCodes.Status200OK, "The order", Type = typeof(OrderDto))]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid orderId, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery { OrderId = orderId }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates a Draft order (optionally with initial line items).</summary>
    [HttpPost]
    [Authorize(Policy = OmsPolicy.CanCreateOrder)]
    [SwaggerOperation(Summary = "Create a new OMS order (Draft)")]
    [SwaggerResponse(StatusCodes.Status201Created, "The created order", Type = typeof(OrderDto))]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetOrderById), new { orderId = result.Id }, result);
    }

    /// <summary>Submits a Draft order for approval (requires at least one item).</summary>
    [HttpPost("{orderId:guid}/submit")]
    [Authorize(Policy = OmsPolicy.CanSubmitOrder)]
    [SwaggerOperation(Summary = "Submit a Draft OMS order")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitOrder(Guid orderId, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new SubmitOrderCommand { OrderId = orderId }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cancels the caller's own order (creator-only; enforced in the handler).</summary>
    [HttpPost("{orderId:guid}/cancel")]
    [Authorize(Policy = OmsPolicy.CanCancelOwnOrder)]
    [SwaggerOperation(Summary = "Cancel own OMS order (creator only)")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(
        Guid orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new CancelOrderCommand
        {
            OrderId = orderId,
            Reason = request.Reason,
            CancelAny = false
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancels any non-final order (Administrator only) — the broader
    /// cancellation permission; ownership is not required.
    /// </summary>
    [HttpPost("{orderId:guid}/cancel-any")]
    [Authorize(Policy = OmsPolicy.CanCancelAnyOrder)]
    [SwaggerOperation(Summary = "Cancel any OMS order (Administrator only)")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelAnyOrder(
        Guid orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new CancelOrderCommand
        {
            OrderId = orderId,
            Reason = request.Reason,
            CancelAny = true
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Adds a line item to a Draft order.</summary>
    [HttpPost("{orderId:guid}/items")]
    [Authorize(Policy = OmsPolicy.CanEditOrder)]
    [SwaggerOperation(Summary = "Add an item to a Draft OMS order")]
    [ProducesResponseType(typeof(OrderItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddOrderItem(
        Guid orderId,
        [FromBody] AddOrderItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new AddOrderItemCommand
        {
            OrderId = orderId,
            ItemCode = request.ItemCode,
            Description = request.Description,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Removes a line item from a Draft order.</summary>
    [HttpDelete("{orderId:guid}/items/{itemId:guid}")]
    [Authorize(Policy = OmsPolicy.CanEditOrder)]
    [SwaggerOperation(Summary = "Remove an item from a Draft OMS order")]
    [ProducesResponseType(typeof(OrderItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveOrderItem(
        Guid orderId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new RemoveOrderItemCommand
        {
            OrderId = orderId,
            ItemId = itemId
        }, cancellationToken);
        return Ok(result);
    }
}

/// <summary>Request body for order cancellation.</summary>
public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Request body for adding a line item to a Draft order.</summary>
public class AddOrderItemRequest
{
    public string? ItemCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}