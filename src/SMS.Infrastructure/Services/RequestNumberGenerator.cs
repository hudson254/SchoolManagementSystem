using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Common;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Concurrency-safe request number generator.
    /// Format: REQ-yyyy-nnnnnn
    /// </summary>
    public class RequestNumberGenerator : IRequestNumberGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RequestNumberGenerator> _logger;

        public RequestNumberGenerator(ApplicationDbContext context, ILogger<RequestNumberGenerator> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateNextRequestNumberAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));

            var year = DateTime.UtcNow.Year;
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

                await using var command = connection.CreateCommand();
                command.CommandText =
                    // This statement bypasses EF, so it must satisfy the table exactly.
                    // sms_request_number_sequences has seven NOT NULL columns with no
                    // database default, because BaseEntity normally populates them:
                    //
                    //   id, tenant_id, "Year", "LastNumber", created_at, updated_at, is_deleted
                    //
                    // Omitting the quoted PascalCase names fails with 42703; omitting id,
                    // created_at, updated_at or is_deleted fails with 23502. The statement
                    // is verified against the real schema in
                    // RequestNumberSequenceSchemaTests.
                    @"INSERT INTO sms_request_number_sequences
                        (id, tenant_id, ""Year"", ""LastNumber"",
                         created_at, updated_at, created_date, is_deleted)
                      VALUES (gen_random_uuid(), @tenant_id, @year, 1,
                              now(), now(), now(), false)
                      ON CONFLICT (tenant_id, ""Year"")
                      DO UPDATE SET ""LastNumber"" = sms_request_number_sequences.""LastNumber"" + 1
                      RETURNING ""LastNumber"";";

                var tenantParam = command.CreateParameter();
                tenantParam.ParameterName = "@tenant_id";
                tenantParam.Value = tenantId;
                command.Parameters.Add(tenantParam);

                var yearParam = command.CreateParameter();
                yearParam.ParameterName = "@year";
                yearParam.Value = year;
                command.Parameters.Add(yearParam);

                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                var sequence = Convert.ToInt64(result);

                var requestNumber = SmsRequestNumber.Format(year, sequence);
                _logger.LogDebug("Allocated request number {RequestNumber} for tenant {TenantId}", requestNumber, tenantId);
                return requestNumber;
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
            parameter.ParameterName = "@tenant_id";
            parameter.Value = tenantId.ToString();
            setCommand.Parameters.Add(parameter);
            await setCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
