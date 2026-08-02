using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Features.PasswordReset.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.PasswordReset
{
    public class RejectPasswordResetCommandTests
    {
        [Fact]
        public async Task Handle_ValidRequest_RejectsRequest()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var adminId = "admin-123";
            var resetRequest = new PasswordResetRequest
            {
                Id = requestId,
                Status = PasswordResetRequestStatus.Pending
            };

            var repoMock = new Mock<IPasswordResetRequestRepository>();
            repoMock.Setup(r => r.GetByIdAsync(requestId)).ReturnsAsync(resetRequest);

            var loggerMock = new Mock<ILogger<RejectPasswordResetCommandHandler>>();

            var handler = new RejectPasswordResetCommandHandler(
                repoMock.Object,
                loggerMock.Object);

            var command = new RejectPasswordResetCommand
            {
                RequestId = requestId,
                AdminUserId = adminId,
                ResolutionNote = "Invalid request"
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(PasswordResetRequestStatus.Rejected, resetRequest.Status);
            Assert.Equal(adminId, resetRequest.FulfilledByUserId);
            Assert.NotNull(resetRequest.FulfilledAt);
            Assert.Equal("Invalid request", resetRequest.ResolutionNote);

            repoMock.Verify(r => r.UpdateAsync(It.Is<PasswordResetRequest>(r => r.Status == PasswordResetRequestStatus.Rejected)), Times.Once);
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var repoMock = new Mock<IPasswordResetRequestRepository>();
            repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PasswordResetRequest?)null);

            var loggerMock = new Mock<ILogger<RejectPasswordResetCommandHandler>>();

            var handler = new RejectPasswordResetCommandHandler(
                repoMock.Object,
                loggerMock.Object);

            var command = new RejectPasswordResetCommand { RequestId = Guid.NewGuid() };

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

            var loggerMock = new Mock<ILogger<RejectPasswordResetCommandHandler>>();

            var handler = new RejectPasswordResetCommandHandler(
                repoMock.Object,
                loggerMock.Object);

            var command = new RejectPasswordResetCommand { RequestId = requestId };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        }
    }
}
