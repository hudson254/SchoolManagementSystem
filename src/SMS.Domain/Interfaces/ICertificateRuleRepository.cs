using System;
using System.Threading;
using System.Threading.Tasks;
using SMS.Domain.Entities;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Repository for managing configurable certificate eligibility rules
    /// </summary>
    public interface ICertificateRuleRepository : IRepository<CertificateRule>
    {
        /// <summary>
        /// Get the currently active certificate rule
        /// </summary>
        Task<CertificateRule?> GetActiveRuleAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the active rule effective at a specific date
        /// </summary>
        Task<CertificateRule?> GetActiveRuleForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    }
}
