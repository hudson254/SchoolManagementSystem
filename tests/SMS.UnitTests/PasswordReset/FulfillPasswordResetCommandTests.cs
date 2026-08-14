using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Features.PasswordReset.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Identity.Services;
using Xunit;

namespace SMS.UnitTests.PasswordReset
{
    public class FulfillPasswordResetCommandTests
    {
        [Fact]
        public async Task Handle_ValidRequest_FulfillsResetAndUpdatesRequest()
        {
            // Arrange
            var userId = "user-123";
            var requestId = Guid.NewGuid();
            var adminId = "admin-456";
            var tempPassword = "TempPass123!";

            var resetRequest = new PasswordResetRequest
            {
                Id = requestId,
                UserId = userId,
                RequestedEmail = "user@example.com",
                Status = PasswordResetRequestStatus.Pending
            };

            var user = new User { Id = userId, Email = "user@example.com" };

            var repoMock = new Mock<IPasswordResetRequestRepository>();
            repoMock.Setup(r => r.GetByIdAsync(requestId)).ReturnsAsync(resetRequest);

            var userManagerMock = new Mock<IUserManagerService>();
            userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
            userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(It.IsAny<User>())).ReturnsAsync("reset-token");
            userManagerMock.Setup(u => u.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            userManagerMock.Setup(u => u.RevokeAllRefreshTokensAsync(It.IsAny<string>())).ReturnsAsync(true);

            var loggerMock = new Mock<ILogger<FulfillPasswordResetCommandHandler>>();

            var handler = new FulfillPasswordResetCommandHandler(
                repoMock.Object,
                userManagerMock.Object,
                loggerMock.Object);

            var command = new FulfillPasswordResetCommand
            {
                RequestId = requestId,
                AdminUserId = adminId,
                ResolutionNote = "Admin reset"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(PasswordResetRequestStatus.Fulfilled, resetRequest.Status);
            Assert.Equal(adminId, resetRequest.FulfilledByUserId);
            Assert.NotNull(resetRequest.FulfilledAt);
            Assert.Equal("Admin reset", resetRequest.ResolutionNote);

            repoMock.Verify(r => r.UpdateAsync(It.Is<PasswordResetRequest>(r => r.Status == PasswordResetRequestStatus.Fulfilled)), Times.Once);
            // Verify that refresh tokens were revoked after password reset
            userManagerMock.Verify(u => u.RevokeAllRefreshTokensAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var repoMock = new Mock<IPasswordResetRequestRepository>();
            repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PasswordResetRequest?)null);

            var userManagerMock = new Mock<IUserManagerService>();
            var loggerMock = new Mock<ILogger<FulfillPasswordResetCommandHandler>>();

            var handler = new FulfillPasswordResetCommandHandler(
                repoMock.Object,
                userManagerMock.Object,
                loggerMock.Object);

            var command = new FulfillPasswordResetCommand { RequestId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_AlreadyFulfilled_ThrowsInvalidOperationException()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var resetRequest = new PasswordResetRequest
            {
                Id = requestId,
                Status = PasswordResetRequestStatus.Fulfilled
            };

            var repoMock = new Mock<IPasswordResetRequestRepository>();
            repoMock.Setup(r => r.GetByIdAsync(requestId)).ReturnsAsync(resetRequest);

            var userManagerMock = new Mock<IUserManagerService>();
            var loggerMock = new Mock<ILogger<FulfillPasswordResetCommandHandler>>();

            var handler = new FulfillPasswordResetCommandHandler(
                repoMock.Object,
                userManagerMock.Object,
                loggerMock.Object);

            var command = new FulfillPasswordResetCommand { RequestId = requestId };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        }
    }
}
