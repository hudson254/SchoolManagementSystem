using Microsoft.Extensions.Logging;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Road Accounting service foundation. Phase 2 implements NO calculations:
    /// allocation, posting, balance changes, road costs, deductions, credits and
    /// debits are all gated behind open requirement #18
    /// (Documentation/OMS/OMS_OPEN_REQUIREMENTS.md). Every operation returns an
    /// explicit <see cref="RoadAccountingResult.NotConfigured"/> result so callers
    /// can never mistake a missing rule for a zero-value accounting result.
    /// </summary>
    public class RoadAccountingService : IRoadAccountingService
    {
        private readonly ILogger<RoadAccountingService> _logger;

        public RoadAccountingService(ILogger<RoadAccountingService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<RoadAccountingResult> ComputeOrderAllocationAsync(Order order, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(order);
            _logger.LogWarning(
                "Road accounting computation requested for order {OrderNumber} but the business rules are not configured; returning NotConfigured (open requirement #18).",
                order.OrderNumber);
            return Task.FromResult(RoadAccountingResult.NotConfigured());
        }

        /// <inheritdoc />
        public Task<RoadAccountingResult> PostOrderToAccountAsync(Order order, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(order);
            _logger.LogWarning(
                "Road accounting posting requested for order {OrderNumber} but the business rules are not configured; no balance was changed (open requirement #18).",
                order.OrderNumber);
            return Task.FromResult(RoadAccountingResult.NotConfigured());
        }
    }
}
