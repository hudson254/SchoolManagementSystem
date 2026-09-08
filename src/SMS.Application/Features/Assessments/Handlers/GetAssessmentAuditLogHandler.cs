using MediatR;
using SMS.Application.Features.Assessments.DTOs;
using SMS.Application.Features.Assessments.Queries;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.Assessments.Handlers
{
    /// <summary>
    /// Returns the assessment audit trail (Administrator). Audit logs are
    /// append-only records produced by the audit service.
    /// </summary>
    public class GetAssessmentAuditLogHandler : IRequestHandler<GetAssessmentAuditLogQuery, IEnumerable<AuditLogDto>>
    {
        private readonly IAuditService _auditService;

        public GetAssessmentAuditLogHandler(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public async Task<IEnumerable<AuditLogDto>> Handle(GetAssessmentAuditLogQuery request, CancellationToken cancellationToken)
        {
            var result = await _auditService.GetAuditLogsAsync(null, null, null, null, null, null, 1, 200);
            var dtos = new List<AuditLogDto>();

            foreach (var log in result.logs)
            {
                if (request.AssessmentId != null && log.EntityId != request.AssessmentId.ToString())
                    continue;
                if (request.UnitId != null && !string.IsNullOrEmpty(log.EntityId) && !MatchesUnit(log, request.UnitId!))
                    continue;

                dtos.Add(new AuditLogDto
                {
                    Id = log.Id,
                    Timestamp = log.CreatedAt,
                    UserId = log.UserId,
                    UserRole = log.UserRole,
                    Action = log.Action,
                    EntityName = log.EntityName,
                    EntityId = log.EntityId,
                    PreviousValue = log.OldValues,
                    NewValue = log.NewValues,
                    Reason = log.FailureReason,
                    IpAddress = log.IPAddress,
                    SessionId = log.SessionId
                });
            }

            return dtos;
        }

        private static bool MatchesUnit(SMS.Domain.Entities.AuditLog log, Guid? unitId)
        {
            // Unit-scoped audit entity IDs are stored as the unit UUID.
            return log.EntityId == unitId.ToString()
                || (log.NewValues ?? string.Empty).Contains(unitId.ToString())
                || (log.OldValues ?? string.Empty).Contains(unitId.ToString());
        }
    }
}