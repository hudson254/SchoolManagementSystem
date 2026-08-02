using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantResolver _tenantResolver;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext context,
            ITenantResolver tenantResolver,
            ICurrentUserService currentUserService,
            ILogger<AuditService> logger)
        {
            _context = context;
            _tenantResolver = tenantResolver;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public string GetCurrentUser()
        {
            return _currentUserService.GetUserId() ?? "SYSTEM";
        }

        public async Task LogAsync(string entityName, string action, Guid entityId, string? oldValues = null, string? newValues = null)
        {
            try
            {
                var tenantId = await _tenantResolver.GetTenantIdAsync();
                var userId = GetCurrentUser();
                var ipAddress = GetClientIp();

                var auditLog = new AuditLog
                {
                    EntityName = entityName,
                    Action = action,
                    EntityId = entityId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    UserId = userId,
                    IPAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    TenantId = tenantId,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit log created: {Action} {EntityName} {EntityId}", action, entityName, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log: {Action} {EntityName} {EntityId}", action, entityName, entityId);
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid? entityId = null, string? entityName = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (entityId.HasValue)
                query = query.Where(l => l.EntityId == entityId.Value);

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(l => l.EntityName == entityName);

            if (fromDate.HasValue)
                query = query.Where(l => l.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.Timestamp <= toDate.Value);

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId)
        {
            return await _context.AuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action)
        {
            return await _context.AuditLogs
                .Where(l => l.Action == action)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetRecentAuditLogsAsync(int count, CancellationToken cancellationToken = default)
        {
            return await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        private string GetClientIp()
        {
            // Implementation would get IP from HttpContext
            return "127.0.0.1";
        }
    }
}