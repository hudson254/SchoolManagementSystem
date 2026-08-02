using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Service for recording and retrieving audit trail entries.
    /// Audit records are immutable and capture all sensitive user actions.
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Logs a generic audit event.
        /// </summary>
        Task LogAsync(string action, string entityName, string details);

        /// <summary>
        /// Logs an activity audit event with entity ID.
        /// </summary>
        Task LogActivityAsync(string action, string entityName, string entityId, string details);

        /// <summary>
        /// Logs a data change audit event with old and new values.
        /// </summary>
        Task LogDataChangeAsync(string entityType, string entityId, string action, string changes);

        /// <summary>
        /// Logs a login event.
        /// </summary>
        Task LogLoginAsync(string userId, string username, bool success, string ipAddress);

        /// <summary>
        /// Logs a logout event.
        /// </summary>
        Task LogLogoutAsync(string userId, string username, string ipAddress);

        /// <summary>
        /// Logs a failed login attempt.
        /// </summary>
        Task LogFailedLoginAsync(string username, string ipAddress, string failureReason);

        /// <summary>
        /// Logs a password reset event.
        /// </summary>
        Task LogPasswordResetAsync(string userId, string username, bool success, string ipAddress);

        /// <summary>
        /// Logs a password change event.
        /// </summary>
        Task LogPasswordChangeAsync(string userId, string username, bool success, string ipAddress);

        /// <summary>
        /// Logs a security event.
        /// </summary>
        Task LogSecurityEventAsync(string eventType, string userId, string details);

        /// <summary>
        /// Logs a performance event.
        /// </summary>
        Task LogPerformanceAsync(string operation, long durationMs, string details = null);

        /// <summary>
        /// Logs an error event.
        /// </summary>
        Task LogErrorAsync(string message, string stackTrace);

        /// <summary>
        /// Gets recent audit logs.
        /// </summary>
        Task<IEnumerable<AuditLog>> GetRecentAuditLogsAsync(int count);

        /// <summary>
        /// Gets audit logs with filtering and pagination.
        /// </summary>
        Task<(IEnumerable<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
            string? userId = null,
            string? action = null,
            string? entityName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? success = null,
            int page = 1,
            int pageSize = 50);
    }
}
