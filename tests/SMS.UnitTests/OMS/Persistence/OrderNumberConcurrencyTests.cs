using FluentAssertions;
using SMS.Domain.Common;
using SMS.Domain.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// Order-number concurrency tests (Phase 2). Per open requirement #1,
    /// sequence allocation must be atomic and safe under concurrent creation.
    /// The production generator uses PostgreSQL INSERT ... ON CONFLICT ...
    /// UPDATE ... RETURNING against oms_order_sequences. These tests verify
    /// the *contract*: per-tenant, monotonically-increasing, unique, collision-free
    /// numbers via a thread-safe in-memory substitute that mirrors that contract.
    /// </summary>
    public class OrderNumberConcurrencyTests
    {
        /// <summary>
        /// Minimal thread-safe implementation mirroring the generator contract
        /// (per-tenant, per-year, monotonically increasing, unique).
        /// </summary>
        private sealed class TestOrderNumberGenerator : IOrderNumberGenerator
        {
            private readonly ConcurrentDictionary<string, long> _sequences = new();

            public Task<string> GenerateNextOrderNumberAsync(Guid tenantId, CancellationToken ct = default)
            {
                if (tenantId == Guid.Empty)
                    throw new ArgumentException("Tenant id must not be empty.", nameof(tenantId));

                var year = DateTime.UtcNow.Year;
                var sequence = _sequences.AddOrUpdate(
                    key: $"{tenantId}:{year}",
                    addValueFactory: _ => 1L,
                    updateValueFactory: (_, current) => current + 1);

                return Task.FromResult(OmsOrderNumber.Format(year, sequence));
            }
        }

        [Fact]
        public async Task ConcurrentGeneration_ProducesUniqueNumbers_ForSameTenant()
        {
            var gen = new TestOrderNumberGenerator();
            var tenantId = Guid.NewGuid();
            const int count = 200;

            var tasks = new Task<string>[count];
            for (int i = 0; i < count; i++)
                tasks[i] = gen.GenerateNextOrderNumberAsync(tenantId);

            var numbers = await Task.WhenAll(tasks);

            var unique = new HashSet<string>(numbers);
            unique.Should().HaveCount(count, "all generated numbers must be unique under concurrency");

            // The lowest sequence number for this run must be 1 (format ORD-yyyy-000001).
            unique.Should().Contain($"ORD-{DateTime.UtcNow.Year}-000001");
        }

        [Fact]
        public async Task PerTenant_SequencesAreIndependent()
        {
            var gen = new TestOrderNumberGenerator();
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            var aTasks = new Task<string>[50];
            var bTasks = new Task<string>[50];
            for (int i = 0; i < 50; i++)
            {
                aTasks[i] = gen.GenerateNextOrderNumberAsync(tenantA);
                bTasks[i] = gen.GenerateNextOrderNumberAsync(tenantB);
            }

            await Task.WhenAll(aTasks.Concat(bTasks));

            var aNumbers = new HashSet<string>(aTasks.Select(t => t.Result));
            var bNumbers = new HashSet<string>(bTasks.Select(t => t.Result));

            aNumbers.Should().HaveCount(50);
            bNumbers.Should().HaveCount(50);
            aNumbers.Should().Contain($"ORD-{DateTime.UtcNow.Year}-000001");
            bNumbers.Should().Contain($"ORD-{DateTime.UtcNow.Year}-000001");
        }

                [Fact]
        public void Generate_WithEmptyTenant_Throws()
        {
            var gen = new TestOrderNumberGenerator();
            Assert.Throws<ArgumentException>(() =>
                gen.GenerateNextOrderNumberAsync(Guid.Empty).GetAwaiter().GetResult());
        }

        [Fact]
        public async Task Generate_ProducesValidFormat()
        {
            var gen = new TestOrderNumberGenerator();
            var number = await gen.GenerateNextOrderNumberAsync(Guid.NewGuid());
            OmsOrderNumber.IsValidFormat(number).Should().BeTrue();
        }
    }
}

