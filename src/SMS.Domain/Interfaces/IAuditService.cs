using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    public interface IAuditService
    {
        string GetCurrentUser();
        Task LogAsync(string entityName, string action, Guid entityId, string? oldValues = null, string? newValues = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid? entityId = null, string? entityName = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId);
        Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action);
    }
}