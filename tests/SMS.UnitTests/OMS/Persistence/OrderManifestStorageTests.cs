using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SMS.Infrastructure.Options;
using SMS.Infrastructure.Storage;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SMS.UnitTests.OMS.Persistence
{
    /// <summary>
    /// OrderManifestStorage safety tests (Phase 2).
    /// Verifies: safe filenames, tenant segmentation, path-traversal rejection,
    /// atomic writes, size limit, read/delete, and retention.
    /// </summary>
    public class OrderManifestStorageTests : IDisposable
    {
        private readonly string _basePath;

        public OrderManifestStorageTests()
        {
            _basePath = Path.Combine(Path.GetTempPath(), $"oms-manifest-{Guid.NewGuid():N}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_basePath))
                Directory.Delete(_basePath, true);
        }

        private OrderManifestStorage CreateStorage(int maxFileSizeMb = 10, int retentionDays = 30)
        {
            var options = Options.Create(new OrderManifestStorageOptions
            {
                Enabled = true,
                BasePath = _basePath,
                FilePrefix = "order-manifest-",
                FileExtension = ".json",
                RetentionDays = retentionDays,
                MaxFileSizeMB = maxFileSizeMb
            });
            return new OrderManifestStorage(options, NullLogger<OrderManifestStorage>.Instance);
        }

        private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

        private const string ManifestContent = "{\"orderId\":\"test\",\"items\":[]}";

        [Fact]
        public async Task Store_StoresWithinTenantSegment()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            var path = await storage.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent));

            path.Should().Be($"{tenant:N}/manifest.json");
            var fullPath = Path.Combine(_basePath, tenant.ToString("N"), "manifest.json");
            File.Exists(fullPath).Should().BeTrue();
        }

        [Fact]
        public async Task Store_ThrowsWrongExtension()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            await storage.Invoking(s => s.StoreAsync(tenant, "manifest.xml", ToStream(ManifestContent)))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("*extension*");
        }

        [Fact]
        public async Task OpenRead_ReturnsNull_WhenFileDoesNotExist()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            var stream = await storage.OpenReadAsync(tenant, $"{tenant:N}/missing.json");
            stream.Should().BeNull();
        }

        [Fact]
        public async Task OpenRead_ReturnsContent_WhenFileExists()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            var path = await storage.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent));
            var stream = await storage.OpenReadAsync(tenant, path);

            stream.Should().NotBeNull();
            using var reader = new StreamReader(stream!);
            (await reader.ReadToEndAsync()).Should().Be(ManifestContent);
        }

        [Fact]
        public async Task ExistsAsync_ReflectsFilePresence()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            var path = await storage.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent));

            (await storage.ExistsAsync(tenant, path)).Should().BeTrue();
            (await storage.ExistsAsync(tenant, $"{tenant:N}/nonexistent.json")).Should().BeFalse();
        }

        [Fact]
        public async Task Delete_RemovesFile()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            var path = await storage.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent));
            await storage.DeleteAsync(tenant, path);

            (await storage.ExistsAsync(tenant, path)).Should().BeFalse();
        }

                [Fact]
        public async Task Store_ThrowsOnDuplicateFile()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            await storage.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent));
            await storage.Invoking(s => s.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent)))
                .Should().ThrowAsync<IOException>();
        }

        [Fact]
        public async Task Store_ThrowsExceedingMaxSize()
        {
            var storage = CreateStorage(maxFileSizeMb: 1);
            var tenant = Guid.NewGuid();
            var largeContent = new string('x', 2 * 1024 * 1024);

            await storage.Invoking(s => s.StoreAsync(tenant, "manifest.json", ToStream(largeContent)))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*exceeds*");
        }

        [Fact]
        public async Task OpenRead_RejectsAbsolutePath()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            await storage.Invoking(s => s.OpenReadAsync(tenant, "/etc/passwd.json"))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task OpenRead_RejectsWrongTenantSegment()
        {
            var storage = CreateStorage();
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            var path = await storage.StoreAsync(tenantA, "manifest.json", ToStream(ManifestContent));

            await storage.Invoking(s => s.OpenReadAsync(tenantB, path))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*does not belong to tenant*");
        }

        [Fact]
        public async Task ApplyRetention_DeletesOldFiles()
        {
            var storage = CreateStorage(retentionDays: 0);
            var tenant = Guid.NewGuid();

            var path = await storage.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent));

            var deleted = await storage.ApplyRetentionAsync();
            deleted.Should().BeGreaterOrEqualTo(1);
            (await storage.ExistsAsync(tenant, path)).Should().BeFalse();
        }

        [Fact]
        public async Task Store_AtomicWrite_NoTempFilesRemain()
        {
            var storage = CreateStorage();
            var tenant = Guid.NewGuid();

            await storage.StoreAsync(tenant, "manifest.json", ToStream(ManifestContent));

            var tenantRoot = Path.Combine(_basePath, tenant.ToString("N"));
            Directory.GetFiles(tenantRoot, ".*.tmp").Should().BeEmpty();
        }

        [Fact]
        public void Store_WhenDisabled_Throws()
        {
            var options = Options.Create(new OrderManifestStorageOptions
            {
                Enabled = false,
                BasePath = _basePath,
                FilePrefix = "order-manifest-",
                FileExtension = ".json",
                RetentionDays = 30,
                MaxFileSizeMB = 10
            });
            var storage = new OrderManifestStorage(options, NullLogger<OrderManifestStorage>.Instance);

            storage.Invoking(s => s.StoreAsync(Guid.NewGuid(), "manifest.json", ToStream(ManifestContent)).Wait())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*disabled*");
        }

        [Fact]
        public async Task Store_TenantSegmentIsIsolated()
        {
            var storage = CreateStorage();
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            await storage.StoreAsync(tenantA, "manifest.json", ToStream(ManifestContent));

            var aPath = $"{tenantA:N}/manifest.json";
            await storage.Invoking(s => s.OpenReadAsync(tenantB, aPath))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}
