using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Common;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Concurrency-safe order number generator (development default format
    /// ORD-yyyy-nnnnnn, open requirement #1).
    ///
    /// Allocates from a per-tenant, per-year sequence row using a single atomic
    /// PostgreSQL statement (INSERT ... ON CONFLICT ... UPDATE ... RETURNING) —
    /// never MAX(existing)+1, which is unsafe under concurrent creation. The
    /// statement is atomic regardless of the ambient transaction, and when called
    /// inside the UnitOfWork transaction it joins it automatically (same
    /// connection), so an order and its number commit or fail together.
    ///
    /// The format lives in <see cref="OmsOrderNumber"/>, so the numbering
    /// strategy can be replaced without touching the Order entity.
    /// </summary>
    public class OrderNumberGenerator : IOrderNumberGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderNumberGenerator> _logger;

        public OrderNumberGenerator(ApplicationDbContext context, ILogger<OrderNumberGenerator> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> GenerateNextOrderNumberAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));
            }

            var year = DateTime.UtcNow.Year;

            // RLS note: oms_order_sequences is protected by row-level security.
            // The EF interceptor sets app.tenant_id only on EF-generated commands,
            // so set it explicitly for this allocation to keep the policy satisfied
            // even when the generator runs before any EF query in the request.
            var connection = _context.Database.GetDbConnection();
            var shouldClose = false;
            if (connection.State != ConnectionState.Open)
            {
                await _context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                shouldClose = true;
            }

            try
            {
                await SetTenantContextAsync(connection, tenantId, cancellationToken).ConfigureAwait(false);

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @"INSERT INTO oms_order_sequences (tenant_id, year, last_number)
                          VALUES (@tenant_id, @year, 1)
                          ON CONFLICT (tenant_id, year)
                          DO UPDATE SET last_number = oms_order_sequences.last_number + 1
                          RETURNING last_number;";

                    var tenantParam = command.CreateParameter();
                    tenantParam.ParameterName = "tenant_id";
                    tenantParam.Value = tenantId;
                    command.Parameters.Add(tenantParam);

                    var yearParam = command.CreateParameter();
                    yearParam.ParameterName = "year";
                    yearParam.Value = year;
                    command.Parameters.Add(yearParam);

                    var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    var sequence = Convert.ToInt64(result);

                    var orderNumber = OmsOrderNumber.Format(year, sequence);
                    _logger.LogDebug("Allocated order number {OrderNumber} for tenant {TenantId}", orderNumber, tenantId);
                    return orderNumber;
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await _context.Database.CloseConnectionAsync().ConfigureAwait(false);
                }
            }
        }

        private static async Task SetTenantContextAsync(System.Data.Common.DbConnection connection, Guid tenantId, CancellationToken cancellationToken)
        {
            await using var setCommand = connection.CreateCommand();
            setCommand.CommandText = "SELECT set_config('app.tenant_id', @tenant_id, false)";
            var parameter = setCommand.CreateParameter();
            parameter.ParameterName = "tenant_id";
            parameter.Value = tenantId.ToString();
            setCommand.Parameters.Add(parameter);
            await setCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
