using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common;
using SMS.Application.Features.Accommodation.Commands;
using SMS.Application.Features.Accommodation.Queries;
using SMS.Application.Features.Reports.Queries;
using SMS.Application.DTOs;

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

        // ===== Lane Management =====

        [HttpGet("lanes")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<LaneDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLanes(
            [FromQuery] string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetLanesQuery { SearchTerm = searchTerm };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("lanes/{id}")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(LaneDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLane(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetLaneQuery { Id = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("lanes")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLane(
            [FromBody] CreateLaneCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetLane), new { id = result }, result);
        }

        [HttpPut("lanes/{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLane(
            Guid id,
            [FromBody] UpdateLaneCommand command,
            CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("lanes/{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLane(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteLaneCommand { Id = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // ===== House Management =====

        [HttpGet("houses")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<HouseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHouses(
            [FromQuery] Guid? laneId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetHousesQuery
            {
                LaneId = laneId,
                SearchTerm = searchTerm,
                Status = status
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("houses/{id}")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(HouseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHouse(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetHouseQuery { Id = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("lanes/{laneId}/houses")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<HouseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLaneHouses(Guid laneId, CancellationToken cancellationToken)
        {
            var query = new GetHousesQuery { LaneId = laneId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("houses")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateHouses(
            [FromBody] CreateHouseCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetHouses), new { laneId = command.LaneId }, result);
        }

        [HttpPut("houses/{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateHouse(
            Guid id,
            [FromBody] UpdateHouseCommand command,
            CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("houses/{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteHouse(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteHouseCommand { Id = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // ===== House Status Management =====

        [HttpPost("houses/{houseId}/maintenance")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetHouseMaintenance(
            Guid houseId,
            [FromBody] SetHouseMaintenanceCommand command,
            CancellationToken cancellationToken)
        {
            command.HouseId = houseId;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("houses/{houseId}/unavailable")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetHouseUnavailable(
            Guid houseId,
            [FromBody] SetHouseUnavailableCommand command,
            CancellationToken cancellationToken)
        {
            command.HouseId = houseId;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpGet("houses/available")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<HouseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableHouses(
            [FromQuery] Guid? laneId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetAvailableHousesQuery { LaneId = laneId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // ===== House Allocation =====

        [HttpPost("houses/{houseId}/assign")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignHouse(
            Guid houseId,
            [FromBody] AssignHouseCommand command,
            CancellationToken cancellationToken)
        {
            command.HouseId = houseId;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("houses/{houseId}/reassign")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReassignHouse(
            Guid houseId,
            [FromBody] ReassignHouseCommand command,
            CancellationToken cancellationToken)
        {
            command.NewHouseId = houseId;
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("houses/{houseId}/vacate")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VacateHouse(Guid houseId, CancellationToken cancellationToken)
        {
            var command = new VacateHouseCommand { HouseId = houseId };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // ===== Dashboard =====

        [HttpGet("dashboard")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(AccommodationDashboardDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            var query = new GetAccommodationDashboardQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // ===== Reports =====

        [HttpGet("reports/lane-occupancy/{laneId}")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(LaneOccupancyReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLaneOccupancyReport(Guid laneId, CancellationToken cancellationToken)
        {
            var query = new GetLaneOccupancyReportQuery { LaneId = laneId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/house-occupancy")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<HouseOccupancyReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHouseOccupancyReport(
            [FromQuery] Guid? laneId = null,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetHouseOccupancyReportQuery { LaneId = laneId, Status = status };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/student-accommodation")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<StudentAccommodationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStudentAccommodationList(
            [FromQuery] Guid? laneId = null,
            [FromQuery] string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetStudentAccommodationListQuery { LaneId = laneId, SearchTerm = searchTerm };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/vacant-houses")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(VacantHouseReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVacantHouseReport(
            [FromQuery] Guid? laneId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetVacantHouseReportQuery { LaneId = laneId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/maintenance")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(MaintenanceReportDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMaintenanceReport(
            [FromQuery] Guid? laneId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetMaintenanceReportQuery { LaneId = laneId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/statistics")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(OccupancyStatisticsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOccupancyStatistics(CancellationToken cancellationToken)
        {
            var query = new GetOccupancyStatisticsQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // ===== Legacy Building/Room endpoints (preserved for backward compatibility) =====

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
            return CreatedAtAction(nameof(GetBuilding), new { id = result }, result);
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
            var query = new SMS.Application.Features.Accommodation.Queries.GetOccupancyReportQuery { BuildingId = buildingId };
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

        [HttpGet("assignments/lecturer/{lecturerId}")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(AccommodationAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLecturerAssignment(Guid lecturerId, CancellationToken cancellationToken)
        {
            var query = new GetLecturerAssignmentQuery { LecturerId = lecturerId };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/lecturer-accommodation")]
        [Authorize(Policy = "ReceptionistAccess")]
        [ProducesResponseType(typeof(IEnumerable<LecturerAccommodationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLecturerAccommodationList(
            [FromQuery] Guid? laneId = null,
            [FromQuery] string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetLecturerAccommodationListQuery { LaneId = laneId, SearchTerm = searchTerm };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
