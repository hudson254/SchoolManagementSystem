using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Domain.Entities;
using SMS.Persistence.Data;
using SMS.Persistence.Repositories;
using Xunit;

namespace SMS.IntegrationTests.Database
{
    public class LoginHistoryRepositoryTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly Mock<ILogger<LoginHistoryRepository>> _loggerMock;

        public LoginHistoryRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _loggerMock = new Mock<ILogger<LoginHistoryRepository>>();
        }

        private LoginHistoryRepository CreateRepository(ApplicationDbContext context)
        {
            return new LoginHistoryRepository(context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetByUserAsync_ShouldReturnOnlyLoginsForUser()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var userId = "user-123";

            var login = new LoginHistory { UserId = userId, LoginTime = DateTime.UtcNow, IsSuccessful = true };
            await repository.AddAsync(login);

            var other = new LoginHistory { UserId = "user-456", LoginTime = DateTime.UtcNow, IsSuccessful = true };
            await repository.AddAsync(other);
            await context.SaveChangesAsync();

            // Act
            var results = await repository.GetByUserAsync(userId);

            // Assert
            results.Should().ContainSingle(h => h.Id == login.Id);
            results.Should().NotContain(h => h.Id == other.Id);
        }

        [Fact]
        public async Task GetLoginCountByUserAsync_ShouldCountOnlySuccessfulLogins()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var userId = "user-123";

            await repository.AddAsync(new LoginHistory { UserId = userId, LoginTime = DateTime.UtcNow, IsSuccessful = true });
            await repository.AddAsync(new LoginHistory { UserId = userId, LoginTime = DateTime.UtcNow, IsSuccessful = true });
            await repository.AddAsync(new LoginHistory { UserId = userId, LoginTime = DateTime.UtcNow, IsSuccessful = false });
            await context.SaveChangesAsync();

            // Act
            var count = await repository.GetLoginCountByUserAsync(userId);

            // Assert
            count.Should().Be(2);
        }

        [Fact]
        public async Task GetFailedLoginsAsync_ShouldReturnOnlyFailedLoginsSinceDate()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);
            var since = DateTime.UtcNow.AddDays(-1);

            var failed = new LoginHistory { UserId = "user-123", LoginTime = DateTime.UtcNow.AddHours(-1), IsSuccessful = false, FailureReason = "Bad password" };
            await repository.AddAsync(failed);

            var successful = new LoginHistory { UserId = "user-123", LoginTime = DateTime.UtcNow.AddHours(-1), IsSuccessful = true };
            await repository.AddAsync(successful);

            var oldFailed = new LoginHistory { UserId = "user-123", LoginTime = DateTime.UtcNow.AddDays(-2), IsSuccessful = false, FailureReason = "Bad password" };
            await repository.AddAsync(oldFailed);
            await context.SaveChangesAsync();

            // Act
            var results = await repository.GetFailedLoginsAsync(since);

            // Assert
            results.Should().ContainSingle(h => h.Id == failed.Id);
            results.Should().NotContain(h => h.Id == successful.Id);
            results.Should().NotContain(h => h.Id == oldFailed.Id);
        }

        [Fact]
        public async Task GetRecentLoginsAsync_ShouldReturnMostRecentFirst()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            var context = _fixture.CreateContext();
            var repository = CreateRepository(context);

            var older = new LoginHistory { UserId = "user-123", LoginTime = DateTime.UtcNow.AddHours(-2), IsSuccessful = true };
            await repository.AddAsync(older);

            var newer = new LoginHistory { UserId = "user-123", LoginTime = DateTime.UtcNow.AddHours(-1), IsSuccessful = true };
            await repository.AddAsync(newer);
            await context.SaveChangesAsync();

            // Act
            var results = (await repository.GetRecentLoginsAsync(1)).ToList();

            // Assert
            results.Should().ContainSingle();
            results[0].Id.Should().Be(newer.Id);
        }
    }
}
