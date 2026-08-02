using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SMS.Application.Features.PasswordReset.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using Xunit;

namespace SMS.UnitTests.PasswordReset
{
    public class GetPendingPasswordResetRequestsQueryTests
    {
        [Fact]
        public async Task Handle_ReturnsPendingRequests()
        {
            // Arrange
            var pendingRequests = new List<PasswordResetRequest>
            {
                new PasswordResetRequest { Id = Guid.NewGuid(), Status = PasswordResetRequestStatus.Pending },
                new PasswordResetRequest { Id = Guid.NewGuid(), Status = PasswordResetRequestStatus.Pending }
            };

            var repoMock = new Mock<IPasswordResetRequestRepository>();
            repoMock.Setup(r => r.GetPendingAsync()).ReturnsAsync(pendingRequests);

            var handler = new GetPendingPasswordResetRequestsQueryHandler(repoMock.Object);

            // Act
            var result = await handler.Handle(new GetPendingPasswordResetRequestsQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(PasswordResetRequestStatus.Pending, r.Status));
        }
    }
}
