using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Enterprise-grade audit service that persists audit records to the database
    /// and logs them via structured logging. Audit records are immutable.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            ILogger<AuditService> logger,
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Creates an AuditLog record with common context information.
        /// </summary>
        private AuditLog CreateAuditLog(string action, string entityName, string? entityId, bool success, string? failureReason = null, string? details = null)
        {
            var context = _httpContextAccessor.HttpContext;
            var userId = context?.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                         ?? context?.User?.Identity?.Name;
            var username = context?.User?.Identity?.Name ?? "System";
            var userRole = context?.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "Unknown";
            var ipAddress = context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context?.Request?.Headers["User-Agent"].ToString();
            // Access Session defensively via ISessionFeature. Accessing HttpContext.Session directly
            // throws InvalidOperationException if session middleware isn't registered (e.g. tests).
            var sessionId = context?.Features?.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session?.Id;
            var correlationId = context?.Items["X-Correlation-ID"]?.ToString();

            return new AuditLog
            {
                UserId = userId,
                Username = username,
                UserRole = userRole,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                SessionId = sessionId,
                CorrelationId = correlationId,
                Success = success,
                FailureReason = failureReason,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
        }

        public async Task LogAsync(string action, string entityName, string details)
        {
            var auditLog = CreateAuditLog(action, entityName, null, true, details: details);
            await PersistAuditLogAsync(auditLog);
            _logger.LogInformation("Audit - Action: {Action}, Entity: {Entity}, Details: {Details}", action, entityName, details);
        }

        public async Task LogActivityAsync(string action, string entityName, string entityId, string details)
        {
            var auditLog = CreateAuditLog(action, entityName, entityId, true, details: details);
            await PersistAuditLogAsync(auditLog);
            _logger.LogInformation("Activity - Action: {Action}, Entity: {Entity}, Id: {EntityId}, Details: {Details}", action, entityName, entityId, details);
        }

        public async Task LogDataChangeAsync(string entityType, string entityId, string action, string changes)
        {
            var auditLog = CreateAuditLog(action, entityType, entityId, true, details: changes);
            await PersistAuditLogAsync(auditLog);
            _logger.LogInformation("Data Change - Entity: {EntityType}, Id: {EntityId}, Action: {Action}, Changes: {Changes}", entityType, entityId, action, changes);
        }

        public async Task LogLoginAsync(string userId, string username, bool success, string ipAddress)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = "Login",
                EntityName = "Authentication",
                Success = success,
                IPAddress = ipAddress,
                UserAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString(),
                SessionId = _httpContextAccessor.HttpContext?.Features?.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session?.Id,
                CorrelationId = _httpContextAccessor.HttpContext?.Items["X-Correlation-ID"]?.ToString(),
                FailureReason = success ? null : "Invalid credentials",
                Timestamp = DateTime.UtcNow
            };
            await PersistAuditLogAsync(auditLog);

            if (success)
                _logger.LogInformation("Login - User: {UserId}, Username: {Username}, IP: {IpAddress}", userId, username, ipAddress);
            else
                _logger.LogWarning("Failed Login - Username: {Username}, IP: {IpAddress}", username, ipAddress);
        }

        public async Task LogLogoutAsync(string userId, string username, string ipAddress)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = "Logout",
                EntityName = "Authentication",
                Success = true,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };
            await PersistAuditLogAsync(auditLog);
            _logger.LogInformation("Logout - User: {UserId}, Username: {Username}, IP: {IpAddress}", userId, username, ipAddress);
        }

        public async Task LogFailedLoginAsync(string username, string ipAddress, string failureReason)
        {
            var auditLog = new AuditLog
            {
                Username = username,
                Action = "FailedLogin",
                EntityName = "Authentication",
                Success = false,
                IPAddress = ipAddress,
                FailureReason = failureReason,
                Timestamp = DateTime.UtcNow
            };
            await PersistAuditLogAsync(auditLog);
            _logger.LogWarning("Failed Login - Username: {Username}, IP: {IpAddress}, Reason: {FailureReason}", username, ipAddress, failureReason);
        }

        public async Task LogPasswordResetAsync(string userId, string username, bool success, string ipAddress)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = "PasswordReset",
                EntityName = "Authentication",
                Success = success,
                IPAddress = ipAddress,
                FailureReason = success ? null : "Password reset failed",
                Timestamp = DateTime.UtcNow
            };
            await PersistAuditLogAsync(auditLog);
            _logger.LogInformation("Password Reset - User: {UserId}, Username: {Username}, Success: {Success}", userId, username, success);
        }

        public async Task LogPasswordChangeAsync(string userId, string username, bool success, string ipAddress)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = "PasswordChange",
                EntityName = "Authentication",
                Success = success,
                IPAddress = ipAddress,
                FailureReason = success ? null : "Password change failed",
                Timestamp = DateTime.UtcNow
            };
            await PersistAuditLogAsync(auditLog);
            _logger.LogInformation("Password Change - User: {UserId}, Username: {Username}, Success: {Success}", userId, username, success);
        }

        public async Task LogSecurityEventAsync(string eventType, string userId, string details)
        {
            var auditLog = CreateAuditLog(eventType, "Security", userId, true, details: details);
            await PersistAuditLogAsync(auditLog);
            _logger.LogWarning("Security Event - Type: {EventType}, User: {UserId}, Details: {Details}", eventType, userId, details);
        }

        public async Task LogPerformanceAsync(string operation, long durationMs, string details = null)
        {
            _logger.LogInformation("Performance - Operation: {Operation}, Duration: {DurationMs}ms, Details: {Details}", operation, durationMs, details);
        }

        public async Task LogErrorAsync(string message, string stackTrace)
        {
            _logger.LogError("Error: {Message}\nStackTrace: {StackTrace}", message, stackTrace);
        }

        public async Task<IEnumerable<AuditLog>> GetRecentAuditLogsAsync(int count)
        {
            try
            {
                return await _dbContext.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent audit logs");
                return new List<AuditLog>();
            }
        }

        public async Task<(IEnumerable<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
            string? userId = null,
            string? action = null,
            string? entityName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? success = null,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(a => a.UserId == userId);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(a => a.Action.Contains(action));

                if (!string.IsNullOrEmpty(entityName))
                    query = query.Where(a => a.EntityName == entityName);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                if (success.HasValue)
                    query = query.Where(a => a.Success == success.Value);

                var totalCount = await query.CountAsync();

                var logs = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (logs, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs with filters");
                return (new List<AuditLog>(), 0);
            }
        }

        /// <summary>
        /// Persists an audit log record to the database.
        /// Audit records are immutable - no updates or deletes are allowed.
        /// </summary>
        private async Task PersistAuditLogAsync(AuditLog auditLog)
        {
            try
            {
                await _dbContext.AuditLogs.AddAsync(auditLog);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // If database persistence fails, log the error but don't fail the request
                _logger.LogError(ex, "Failed to persist audit log. Action: {Action}, Entity: {Entity}", auditLog.Action, auditLog.EntityName);
            }
        }
    }
}
