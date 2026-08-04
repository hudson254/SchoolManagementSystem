using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;
using SMS.Infrastructure.Services;

namespace SMS.Notifications.Services
{
    /// <summary>
    /// Real SMS delivery service using a configurable HTTP-based SMS provider.
    /// Replaces the previous stub that only logged messages.
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly SmsOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SmsService> _logger;

        public SmsService(
            IOptions<SmsOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<SmsService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogWarning("SMS not sent: phone number is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("SMS not sent: message is empty");
                return false;
            }

            if (!_options.Enabled)
            {
                _logger.LogWarning("SMS service is disabled. SMS NOT sent to {PhoneNumber}", phoneNumber);
                return false;
            }

            if (string.IsNullOrEmpty(_options.AccountSid) ||
                string.IsNullOrEmpty(_options.AuthToken) ||
                string.IsNullOrEmpty(_options.FromNumber) ||
                string.IsNullOrEmpty(_options.BaseUrl))
            {
                _logger.LogWarning("SMS service not configured (missing AccountSid/AuthToken/FromNumber/BaseUrl). SMS NOT sent to {PhoneNumber}", phoneNumber);
                return false;
            }

            try
            {
                return await RetryPolicyHelper.ExecuteExternalAsync(
                    async () =>
                    {
                        var client = _httpClientFactory.CreateClient("SmsClient");
                        var requestUri = BuildRequestUri(phoneNumber, message);
                        var response = await client.PostAsync(requestUri, null);
                        response.EnsureSuccessStatusCode();
                        return true;
                    },
                    _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendBulkSmsAsync(string[] phoneNumbers, string message)
        {
            if (phoneNumbers == null || phoneNumbers.Length == 0)
            {
                _logger.LogWarning("Bulk SMS not sent: no phone numbers provided");
                return false;
            }

            var successCount = 0;
            var failCount = 0;

            foreach (var phoneNumber in phoneNumbers)
            {
                var result = await SendSmsAsync(phoneNumber, message);
                if (result)
                    successCount++;
                else
                    failCount++;
            }

            _logger.LogInformation("Bulk SMS completed: {SuccessCount} succeeded, {FailCount} failed", successCount, failCount);
            return failCount == 0;
        }

        private Uri BuildRequestUri(string phoneNumber, string message)
        {
            // Twilio-style URL: {BaseUrl}/{AccountSid}/Messages.json with form-encoded body
            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/{_options.AccountSid}/Messages.json";
            var builder = new UriBuilder(url);
            builder.Query = $"To={Uri.EscapeDataString(phoneNumber)}&From={Uri.EscapeDataString(_options.FromNumber)}&Body={Uri.EscapeDataString(message)}";
            return builder.Uri;
        }
    }
}
