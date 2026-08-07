using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Application.Common;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Represents a persisted error record in the searchable error repository.
    /// This is the PRIVATE error layer — accessible only to authorized administrators.
    /// </summary>
    public class ErrorRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string? CorrelationId { get; set; }
        public string? RequestId { get; set; }
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserRole { get; set; }
        public string? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string? Route { get; set; }
        public string? Endpoint { get; set; }
        public string? HttpMethod { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? FullStackTrace { get; set; }
        public string? SourceFile { get; set; }
        public int? LineNumber { get; set; }
        public string? Namespace { get; set; }
        public string? Assembly { get; set; }
        public string? Method { get; set; }
        public ErrorCategory Category { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string? RootCause { get; set; }
        public long? RequestDurationMs { get; set; }
        public long? MemoryUsageBytes { get; set; }
        public string? ThreadId { get; set; }
        public string? ResolutionStatus { get; set; } = "Open";
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Provides searchable, paginated access to the persisted error repository.
    /// </summary>
    public interface IErrorRepository
    {
        Task<ErrorRecord> AddAsync(ErrorRecord record);
        Task<ErrorRecord?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<ErrorRecord>> SearchAsync(
            DateTime? from = null,
            DateTime? to = null,
            string? userId = null,
            string? tenantId = null,
            string? module = null,
            ErrorCategory? category = null,
            ErrorSeverity? severity = null,
            string? route = null,
            string? correlationId = null,
            string? sessionId = null,
            string? keyword = null,
            string? exceptionType = null,
            int page = 1,
            int pageSize = 50,
            string? sortBy = null,
            bool sortDescending = true);
        Task<int> CountAsync(
            DateTime? from = null,
            DateTime? to = null,
            string? userId = null,
            string? tenantId = null,
            string? module = null,
            ErrorCategory? category = null,
            ErrorSeverity? severity = null,
            string? route = null,
            string? correlationId = null,
            string? sessionId = null,
            string? keyword = null,
            string? exceptionType = null);
        Task UpdateAsync(ErrorRecord record);
        Task<IReadOnlyList<ErrorRecord>> GetRecentAsync(int count = 50);
    }

    /// <summary>
    /// In-memory implementation of <see cref="IErrorRepository"/>.
    /// Suitable for single-instance deployments. Replace with a persistent
    /// database-backed implementation for horizontal scaling.
    /// </summary>
    public class InMemoryErrorRepository : IErrorRepository
    {
        private readonly object _lock = new();
        private readonly List<ErrorRecord> _records = new();
        private readonly ILogger<InMemoryErrorRepository> _logger;

        public InMemoryErrorRepository(ILogger<InMemoryErrorRepository> logger)
        {
            _logger = logger;
        }

        public Task<ErrorRecord> AddAsync(ErrorRecord record)
        {
            lock (_lock)
            {
                _records.Add(record);
            }
            _logger.LogInformation("Error record {ErrorId} persisted with correlation {CorrelationId}", record.Id, record.CorrelationId);
            return Task.FromResult(record);
        }

        public Task<ErrorRecord?> GetByIdAsync(Guid id)
        {
            lock (_lock)
            {
                return Task.FromResult(_records.FirstOrDefault(r => r.Id == id));
            }
        }

        public Task<IReadOnlyList<ErrorRecord>> SearchAsync(
            DateTime? from = null,
            DateTime? to = null,
            string? userId = null,
            string? tenantId = null,
            string? module = null,
            ErrorCategory? category = null,
            ErrorSeverity? severity = null,
            string? route = null,
            string? correlationId = null,
            string? sessionId = null,
            string? keyword = null,
            string? exceptionType = null,
            int page = 1,
            int pageSize = 50,
            string? sortBy = null,
            bool sortDescending = true)
        {
            lock (_lock)
            {
                var query = _records.AsQueryable();

                if (from.HasValue)
                    query = query.Where(r => r.TimestampUtc >= from.Value);
                if (to.HasValue)
                    query = query.Where(r => r.TimestampUtc <= to.Value);
                if (!string.IsNullOrWhiteSpace(userId))
                    query = query.Where(r => r.UserId == userId);
                if (!string.IsNullOrWhiteSpace(tenantId))
                    query = query.Where(r => r.TenantId == tenantId);
                if (!string.IsNullOrWhiteSpace(module))
                    query = query.Where(r => (r.Namespace ?? string.Empty).Contains(module, StringComparison.OrdinalIgnoreCase));
                if (category.HasValue)
                    query = query.Where(r => r.Category == category.Value);
                if (severity.HasValue)
                    query = query.Where(r => r.Severity == severity.Value);
                if (!string.IsNullOrWhiteSpace(route))
                    query = query.Where(r => (r.Route ?? string.Empty).Contains(route, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(correlationId))
                    query = query.Where(r => r.CorrelationId == correlationId);
                if (!string.IsNullOrWhiteSpace(sessionId))
                    query = query.Where(r => r.SessionId == sessionId);
                if (!string.IsNullOrWhiteSpace(exceptionType))
                    query = query.Where(r => (r.ExceptionType ?? string.Empty).Contains(exceptionType, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.ToLowerInvariant();
                    query = query.Where(r =>
                        (r.ExceptionMessage ?? string.Empty).ToLowerInvariant().Contains(kw) ||
                        (r.RootCause ?? string.Empty).ToLowerInvariant().Contains(kw) ||
                        (r.ExceptionType ?? string.Empty).ToLowerInvariant().Contains(kw));
                }

                // Apply sorting
                query = sortBy?.ToLowerInvariant() switch
                {
                    "timestamp" => sortDescending ? query.OrderByDescending(r => r.TimestampUtc) : query.OrderBy(r => r.TimestampUtc),
                    "severity" => sortDescending ? query.OrderByDescending(r => r.Severity) : query.OrderBy(r => r.Severity),
                    "category" => sortDescending ? query.OrderByDescending(r => r.Category) : query.OrderBy(r => r.Category),
                    "username" => sortDescending ? query.OrderByDescending(r => r.Username) : query.OrderBy(r => r.Username),
                    _ => query.OrderByDescending(r => r.TimestampUtc)
                };

                var result = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult<IReadOnlyList<ErrorRecord>>(result);
            }
        }

        public Task<int> CountAsync(
            DateTime? from = null,
            DateTime? to = null,
            string? userId = null,
            string? tenantId = null,
            string? module = null,
            ErrorCategory? category = null,
            ErrorSeverity? severity = null,
            string? route = null,
            string? correlationId = null,
            string? sessionId = null,
            string? keyword = null,
            string? exceptionType = null)
        {
            lock (_lock)
            {
                var query = _records.AsQueryable();

                if (from.HasValue)
                    query = query.Where(r => r.TimestampUtc >= from.Value);
                if (to.HasValue)
                    query = query.Where(r => r.TimestampUtc <= to.Value);
                if (!string.IsNullOrWhiteSpace(userId))
                    query = query.Where(r => r.UserId == userId);
                if (!string.IsNullOrWhiteSpace(tenantId))
                    query = query.Where(r => r.TenantId == tenantId);
                if (!string.IsNullOrWhiteSpace(module))
                    query = query.Where(r => (r.Namespace ?? string.Empty).Contains(module, StringComparison.OrdinalIgnoreCase));
                if (category.HasValue)
                    query = query.Where(r => r.Category == category.Value);
                if (severity.HasValue)
                    query = query.Where(r => r.Severity == severity.Value);
                if (!string.IsNullOrWhiteSpace(route))
                    query = query.Where(r => (r.Route ?? string.Empty).Contains(route, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(correlationId))
                    query = query.Where(r => r.CorrelationId == correlationId);
                if (!string.IsNullOrWhiteSpace(sessionId))
                    query = query.Where(r => r.SessionId == sessionId);
                if (!string.IsNullOrWhiteSpace(exceptionType))
                    query = query.Where(r => (r.ExceptionType ?? string.Empty).Contains(exceptionType, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.ToLowerInvariant();
                    query = query.Where(r =>
                        (r.ExceptionMessage ?? string.Empty).ToLowerInvariant().Contains(kw) ||
                        (r.RootCause ?? string.Empty).ToLowerInvariant().Contains(kw) ||
                        (r.ExceptionType ?? string.Empty).ToLowerInvariant().Contains(kw));
                }

                return Task.FromResult(query.Count());
            }
        }

        public Task UpdateAsync(ErrorRecord record)
        {
            lock (_lock)
            {
                var existing = _records.FirstOrDefault(r => r.Id == record.Id);
                if (existing != null)
                {
                    existing.ResolutionStatus = record.ResolutionStatus;
                    existing.AssignedTo = record.AssignedTo;
                    existing.Notes = record.Notes;
                }
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ErrorRecord>> GetRecentAsync(int count = 50)
        {
            lock (_lock)
            {
                var result = _records
                    .OrderByDescending(r => r.TimestampUtc)
                    .Take(count)
                    .ToList();
                return Task.FromResult<IReadOnlyList<ErrorRecord>>(result);
            }
        }
    }
}
