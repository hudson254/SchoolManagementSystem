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

        /// <summary>Collapses all whitespace so assertions survive SQL reformatting.</summary>
        private static string Normalise(string source) =>
            System.Text.RegularExpressions.Regex.Replace(source, @"\s+", " ");

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
        /// Whitespace is normalised first so reformatting the statement cannot silently
        /// invalidate the guard.
        /// </summary>
        [Fact]
        public void NumberGenerator_QuotesYearAndLastNumberInItsSql()
        {
            var source = Normalise(File.ReadAllText(LocateGeneratorSource()));

            source.Should().Contain(@"""Year""", "the sequence SQL must quote the Year column");
            source.Should().Contain(@"""LastNumber""", "the sequence SQL must quote the LastNumber column");

            // The id column is NOT NULL with no database default. EF normally supplies
            // it client-side, but this statement bypasses EF, so it must set it.
            source.Should().Contain(
                "sms_request_number_sequences (id, tenant_id,",
                "the raw INSERT must supply id explicitly, because the column is NOT NULL " +
                "and has no database default; omitting it fails with 23502.");
            source.Should().Contain("gen_random_uuid()");

            // The remaining BaseEntity audit columns are NOT NULL with no default either.
            foreach (var auditColumn in new[] { "created_at", "updated_at", "is_deleted" })
            {
                source.Should().Contain(
                    auditColumn,
                    "{0} is NOT NULL with no database default, so the raw INSERT must set it",
                    auditColumn);
            }

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
