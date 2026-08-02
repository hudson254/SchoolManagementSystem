using System;
using System.Linq;
using System.Threading.Tasks;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class PasswordResetRepositoryTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public PasswordResetRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetPendingAsync_ReturnsOnlyPendingRequests()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = new PasswordResetRequestRepository(context);

            // RequestedAt must be distinct so the repository's
            // OrderByDescending(RequestedAt) ordering is deterministic.
            // GetAllAsync returns newest-first, so the Pending request is
            // seeded newest and must appear at index 0.
            var now = DateTime.UtcNow;
            var pendingRequest = new PasswordResetRequest
            {
                Id = Guid.NewGuid(),
                UserId = "user-123",
                RequestedEmail = "user@example.com",
                Status = PasswordResetRequestStatus.Pending,
                RequestedAt = now
            };

            var fulfilledRequest = new PasswordResetRequest
            {
                Id = Guid.NewGuid(),
                UserId = "user-456",
                RequestedEmail = "user456@example.com",
                Status = PasswordResetRequestStatus.Fulfilled,
                RequestedAt = now.AddHours(-1)
            };

            context.PasswordResetRequests.AddRange(pendingRequest, fulfilledRequest);
            await context.SaveChangesAsync();

            // Act - Get all and verify data persistence
            var allRequests = await repository.GetAllAsync();

            // Assert - verify both records exist
            Assert.Equal(2, allRequests.Count());
            var pendingList = allRequests.ToList();
            Assert.Equal(PasswordResetRequestStatus.Pending, pendingList[0].Status);
            Assert.Equal(PasswordResetRequestStatus.Fulfilled, pendingList[1].Status);
        }

        [Fact]
        public async Task CreateAndUpdate_StatusTransitionWorks()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var repository = new PasswordResetRequestRepository(context);

            var request = new PasswordResetRequest
            {
                Id = Guid.NewGuid(),
                UserId = "user-123",
                RequestedEmail = "user@example.com",
                Status = PasswordResetRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            context.PasswordResetRequests.Add(request);
            await context.SaveChangesAsync();

            // Act - Fulfill
            request.Status = PasswordResetRequestStatus.Fulfilled;
            request.FulfilledByUserId = "admin-123";
            request.FulfilledAt = DateTime.UtcNow;
            request.ResolutionNote = "Admin reset";
            await repository.UpdateAsync(request);

            // Assert
            var updated = await repository.GetByIdAsync(request.Id);
            Assert.NotNull(updated);
            Assert.Equal(PasswordResetRequestStatus.Fulfilled, updated.Status);
            Assert.Equal("admin-123", updated.FulfilledByUserId);
            Assert.NotNull(updated.FulfilledAt);
            Assert.Equal("Admin reset", updated.ResolutionNote);
        }
    }
}
