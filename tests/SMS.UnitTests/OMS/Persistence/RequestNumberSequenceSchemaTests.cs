using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Persistence.Data;
using System;
using System.IO;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// Guards the identifier casing of the request-number allocation SQL.
    ///
    /// <para>
    /// <c>RequestNumberGenerator</c> allocates request numbers with hand-written SQL
    /// against <c>sms_request_number_sequences</c>. That table is created by
    /// <c>AddSmsRequests</c> with PascalCase columns <c>"Year"</c> and
    /// <c>"LastNumber"</c>. The SQL originally used unquoted lowercase names, which
    /// PostgreSQL folds to <c>year</c>/<c>last_number</c>, so request creation failed
    /// in production with
    /// <c>42703: column "year" of relation "sms_request_number_sequences" does not exist</c>.
    /// </para>
    ///
    /// <para>
    /// Unit tests did not catch it because the generator is exercised against the EF
    /// InMemory provider, which does not parse raw SQL. This test asserts the emitted
    /// SQL matches the columns the migration actually created.
    /// </para>
    /// </summary>
    public class RequestNumberSequenceSchemaTests
    {
        private static ApplicationDbContext CreateDesignTimeContext()
            => new ApplicationDbContextFactory().CreateDbContext(Array.Empty<string>());

        /// <summary>The migrated table really does use PascalCase for these columns.</summary>
        [Fact]
        public void NumberSequenceTable_UsesPascalCaseYearAndLastNumber()
        {
            using var context = CreateDesignTimeContext();

            var table = context.Model
                .GetRelationalModel()
                .Tables
                .First(t => t.Name == "sms_request_number_sequences");

            table.Columns.Select(c => c.Name).Should().Contain("Year");
            table.Columns.Select(c => c.Name).Should().Contain("LastNumber");
        }

        /// <summary>
        /// The generator's SQL must quote those identifiers. InMemory cannot parse raw
        /// SQL, so the statement is asserted textually against the real column names.
        /// </summary>
        [Fact]
        public void NumberGenerator_QuotesYearAndLastNumberInItsSql()
        {
            var source = File.ReadAllText(LocateGeneratorSource());

            source.Should().Contain(@"""Year""", "the sequence SQL must quote the Year column");
            source.Should().Contain(@"""LastNumber""", "the sequence SQL must quote the LastNumber column");

            source.Should().NotContain(
                "sms_request_number_sequences (tenant_id, year, last_number)",
                "unquoted lowercase identifiers fold to names the migration never created");
        }

        private static string LocateGeneratorSource()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(
                    dir.FullName, "src", "SMS.Infrastructure", "Services", "RequestNumberGenerator.cs");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new FileNotFoundException(
                "Could not locate RequestNumberGenerator.cs from " + AppContext.BaseDirectory);
        }
    }
}
