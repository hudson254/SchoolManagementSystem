using FluentValidation;
using SMS.Shared.DTOs;

using SMS.Domain.Interfaces;
using SMS.Application.DTOs;
using Microsoft.Extensions.Logging;
using MediatR;
namespace SMS.Application.Features.Dashboard.Queries
{
    public class GetRecentActivitiesQuery : IRequest<IEnumerable<ActivityDto>>
    {
        public int Count { get; set; } = 10;
    }

    public class GetRecentActivitiesQueryHandler : IRequestHandler<GetRecentActivitiesQuery, IEnumerable<ActivityDto>>
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<GetRecentActivitiesQueryHandler> _logger;

        public GetRecentActivitiesQueryHandler(
            IAuditService auditService,
            ILogger<GetRecentActivitiesQueryHandler> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<ActivityDto>> Handle(GetRecentActivitiesQuery request, CancellationToken cancellationToken)
        {
            var logs = await _auditService.GetRecentAuditLogsAsync(request.Count);

            return logs.Select(l => new ActivityDto
            {
                Message = $"{l.Action} {l.EntityName}",
                User = l.UserId ?? "System",
                Timestamp = l.Timestamp,
                Icon = GetIconForAction(l.Action),
                Color = GetColorForAction(l.Action),
                Status = GetStatusForAction(l.Action),
                Link = $"/{l.EntityName.ToLower()}/{l.EntityId}"
            });
        }

        private string GetIconForAction(string action)
        {
            return action switch
            {
                "Create" => "add",
                "Update" => "edit",
                "Delete" => "delete",
                "Login" => "login",
                "Logout" => "logout",
                "Submit" => "submit",
                "Grade" => "grade",
                "Assign" => "assign",
                "Transfer" => "transfer",
                "Vacate" => "vacate",
                "Enroll" => "enroll",
                "Drop" => "drop",
                "Verify" => "verify",
                _ => "event"
            };
        }

        private string GetColorForAction(string action)
        {
            return action switch
            {
                "Create" => "#4caf50",
                "Update" => "#2196f3",
                "Delete" => "#f44336",
                "Login" => "#4caf50",
                "Logout" => "#ff9800",
                "Submit" => "#9c27b0",
                "Grade" => "#3f51b5",
                "Assign" => "#009688",
                "Transfer" => "#ff5722",
                "Vacate" => "#795548",
                "Enroll" => "#8bc34a",
                "Drop" => "#f44336",
                "Verify" => "#4caf50",
                _ => "#576426"
            };
        }

        private string GetStatusForAction(string action)
        {
            return action switch
            {
                "Create" => "Completed",
                "Update" => "Completed",
                "Delete" => "Completed",
                "Login" => "Success",
                "Logout" => "Completed",
                "Submit" => "Pending",
                "Grade" => "Completed",
                "Assign" => "Completed",
                "Transfer" => "Completed",
                "Vacate" => "Completed",
                "Enroll" => "Completed",
                "Drop" => "Completed",
                "Verify" => "Completed",
                _ => "Completed"
            };
        }
    }
}



