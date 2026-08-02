using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Application.Features.Accommodation.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    public class AccommodationController : BaseApiController
    {
        private readonly ILogger<AccommodationController> _logger;

        public AccommodationController(ILogger<AccommodationController> logger)
        {
            _logger = logger;
        }

        [HttpGet("buildings")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<BuildingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBuildings(CancellationToken cancellationToken)
        {
            var query = new GetBuildingsQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("buildings/{id}")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(BuildingDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBuilding(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetBuildingQuery { BuildingId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("buildings")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(BuildingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateBuilding(
            [FromBody] CreateBuildingCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetBuilding), new { id = result.Id }, result);
        }

        [HttpGet("rooms")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(PagedResult<RoomDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRooms(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? buildingId = null,
            [FromQuery] Guid? blockId = null,
            [FromQuery] bool? isAvailable = null,
            [FromQuery] bool? isOccupied = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetRoomsQuery
            {
                Page = page,
                PageSize = pageSize,
                BuildingId = buildingId,
                BlockId = blockId,
                IsAvailable = isAvailable,
                IsOccupied = isOccupied
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("rooms/available")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableRooms(
            [FromQuery] Guid? buildingId = null,
            [FromQuery] Guid? blockId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAvailableRoomsQuery
            {
                BuildingId = buildingId,
                BlockId = blockId
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("rooms/{id}/assign")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(AccommodationAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRoom(
            Guid id,
            [FromBody] AssignRoomCommand command,
            CancellationToken cancellationToken)
        {
            command.RoomId = id;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("assignments/{id}/transfer")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(AccommodationAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TransferRoom(
            Guid id,
            [FromBody] TransferRoomCommand command,
            CancellationToken cancellationToken)
        {
            command.AssignmentId = id;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("assignments/{id}/vacate")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VacateRoom(Guid id, CancellationToken cancellationToken)
        {
            var command = new VacateRoomCommand { AssignmentId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpGet("reports/occupancy")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(OccupancyReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOccupancyReport(
            [FromQuery] Guid? buildingId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetOccupancyReportQuery { BuildingId = buildingId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/vacant")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(VacantRoomsReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVacantRoomsReport(
            [FromQuery] Guid? buildingId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetVacantRoomsReportQuery { BuildingId = buildingId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("assignments/student/{studentId}")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(AccommodationAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStudentAssignment(Guid studentId, CancellationToken cancellationToken)
        {
            var query = new GetStudentAssignmentQuery { StudentId = studentId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}