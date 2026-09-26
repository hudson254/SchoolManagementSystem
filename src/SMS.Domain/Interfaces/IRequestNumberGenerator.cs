using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Generates tenant-unique, immutable SMS request numbers.
    /// Format: REQ-<yyyy>-<6-digit per-tenant sequence> e.g. REQ-2026-000001.
    /// </summary>
    public interface IRequestNumberGenerator
    {
        Task<string> GenerateNextRequestNumberAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }
}
