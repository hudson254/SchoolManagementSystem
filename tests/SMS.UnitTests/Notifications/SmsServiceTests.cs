using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SMS.Infrastructure.Options;
using SMS.Notifications.Services;
using Xunit;

namespace SMS.UnitTests.Notifications
{
    public class SmsServiceTests
    {
        private readonly SmsOptions _options = new SmsOptions
        {
            Enabled = true,
            AccountSid = "AC123",
            AuthToken = "token123",
            FromNumber = "+15551234567",
            BaseUrl = "https://api.twilio.com/2010-04-01"
        };

        private static IOptions<SmsOptions> BuildOptions(SmsOptions options)
            => Options.Create(options);

        private static IHttpClientFactory BuildHttpClientFactory(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
            return factory.Object;
        }

        private static HttpMessageHandler BuildSuccessHandler()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            return mock.Object;
        }

        private static HttpMessageHandler BuildFailureHandler()
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            return mock.Object;
        }

        [Fact]
        public async Task SendSmsAsync_WithEmptyPhoneNumber_ReturnsFalse()
        {
            var service = new SmsService(
                BuildOptions(_options),
                BuildHttpClientFactory(BuildSuccessHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendSmsAsync("", "Hello");

            Assert.False(result);
        }

        [Fact]
        public async Task SendSmsAsync_WithEmptyMessage_ReturnsFalse()
        {
            var service = new SmsService(
                BuildOptions(_options),
                BuildHttpClientFactory(BuildSuccessHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendSmsAsync("+15551234567", "");

            Assert.False(result);
        }

        [Fact]
        public async Task SendSmsAsync_WhenDisabled_ReturnsFalse()
        {
            var disabled = new SmsOptions { Enabled = false };
            var service = new SmsService(
                BuildOptions(disabled),
                BuildHttpClientFactory(BuildSuccessHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendSmsAsync("+15551234567", "Hello");

            Assert.False(result);
        }

        [Fact]
        public async Task SendSmsAsync_WhenNotConfigured_ReturnsFalse()
        {
            var notConfigured = new SmsOptions
            {
                Enabled = true,
                AccountSid = "",
                AuthToken = "",
                FromNumber = "",
                BaseUrl = ""
            };
            var service = new SmsService(
                BuildOptions(notConfigured),
                BuildHttpClientFactory(BuildSuccessHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendSmsAsync("+15551234567", "Hello");

            Assert.False(result);
        }

        [Fact]
        public async Task SendSmsAsync_WhenConfigured_ReturnsTrue()
        {
            var service = new SmsService(
                BuildOptions(_options),
                BuildHttpClientFactory(BuildSuccessHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendSmsAsync("+15551234567", "Hello");

            Assert.True(result);
        }

        [Fact]
        public async Task SendSmsAsync_WhenProviderFails_ReturnsFalse()
        {
            var service = new SmsService(
                BuildOptions(_options),
                BuildHttpClientFactory(BuildFailureHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendSmsAsync("+15551234567", "Hello");

            Assert.False(result);
        }

        [Fact]
        public async Task SendBulkSmsAsync_WithNoNumbers_ReturnsFalse()
        {
            var service = new SmsService(
                BuildOptions(_options),
                BuildHttpClientFactory(BuildSuccessHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendBulkSmsAsync(Array.Empty<string>(), "Hello");

            Assert.False(result);
        }

        [Fact]
        public async Task SendBulkSmsAsync_WithValidNumbers_ReturnsTrue()
        {
            var service = new SmsService(
                BuildOptions(_options),
                BuildHttpClientFactory(BuildSuccessHandler()),
                NullLogger<SmsService>.Instance);

            var result = await service.SendBulkSmsAsync(
                new[] { "+15551234567", "+15559876543" },
                "Hello");

            Assert.True(result);
        }

        [Fact]
        public async Task SendBulkSmsAsync_WithOneFailure_ReturnsFalse()
        {
            // The phone number is embedded in the request query string (To=...).
            // Fail for the second recipient only, so the bulk send should report
            // overall failure while still returning true for the first recipient.
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
                {
                    var query = request.RequestUri?.Query ?? "";
                    return query.Contains("9876543")
                        ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        : new HttpResponseMessage(HttpStatusCode.OK);
                });

            var service = new SmsService(
                BuildOptions(_options),
                BuildHttpClientFactory(mock.Object),
                NullLogger<SmsService>.Instance);

            var result = await service.SendBulkSmsAsync(
                new[] { "+15551234567", "+15559876543" },
                "Hello");

            Assert.False(result);
        }
    }
}
