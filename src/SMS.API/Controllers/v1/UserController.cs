using SMS.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.DTOs;
using SMS.Application.Features.Users.Commands;
using SMS.Application.Features.Users.Queries;

namespace SMS.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : BaseApiController
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all users with pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUsersQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Role = role,
                IsActive = isActive
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUserQuery { UserId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }

        /// <summary>
        /// Update a user
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(
            Guid id,
            [FromBody] UpdateUserCommand command,
            CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Delete a user (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteUserCommand { UserId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Assign roles to a user
        /// </summary>
        [HttpPost("{id}/roles")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignRoles(
            Guid id,
            [FromBody] AssignRolesCommand command,
            CancellationToken cancellationToken)
        {
            command.UserId = id;
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Remove roles from a user
        /// </summary>
        [HttpDelete("{id}/roles")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveRoles(
            Guid id,
            [FromBody] RemoveRolesCommand command,
            CancellationToken cancellationToken)
        {
            command.UserId = id;
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Activate a user account
        /// </summary>
        [HttpPost("{id}/activate")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateUser(Guid id, CancellationToken cancellationToken)
        {
            var command = new ActivateUserCommand { UserId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Deactivate a user account
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeactivateUserCommand { UserId = id };
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetUserPassword(
            Guid id,
            [FromBody] ResetUserPasswordCommand command,
            CancellationToken cancellationToken)
        {
            command.UserId = id;
            await Mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Get user roles
        /// </summary>
        [HttpGet("{id}/roles")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserRoles(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUserRolesQuery { UserId = id };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get user login history
        /// </summary>
        [HttpGet("{id}/login-history")]
        [Authorize(Policy = "AdministratorAccess")]
        [ProducesResponseType(typeof(IEnumerable<LoginHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserLoginHistory(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserLoginHistoryQuery
            {
                UserId = id,
                Page = page,
                PageSize = pageSize
            };
            var result = await Mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
