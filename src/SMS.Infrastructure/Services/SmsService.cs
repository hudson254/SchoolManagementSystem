using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;

namespace SMS.Infrastructure.Services
{
    /// <summary>
    /// Sends SMS messages through a configurable HTTP provider (Twilio-compatible
    /// REST API). The provider is enabled via <see cref="SmsOptions.Enabled"/>.
    /// When disabled, sends are no-ops (logged) so the system can run without
    /// an SMS provider configured. All provider calls are wrapped in
    /// <see cref="RetryPolicyHelper.ExecuteExternalAsync{T}"/> for resilience
    /// against transient network failures.
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
            // Validate inputs
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogWarning("SMS send skipped: phone number is empty.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("SMS send skipped: message is empty for {PhoneNumber}.", phoneNumber);
                return false;
            }

            // No-op when disabled / not configured
            if (!_options.Enabled)
            {
                _logger.LogInformation("SMS disabled - would send to {PhoneNumber}: {Message}", phoneNumber, message);
                return true;
            }

            if (string.IsNullOrWhiteSpace(_options.AccountSid) ||
                string.IsNullOrWhiteSpace(_options.AuthToken) ||
                string.IsNullOrWhiteSpace(_options.FromNumber))
            {
                _logger.LogWarning("SMS provider not fully configured (AccountSid/AuthToken/FromNumber). Skipping send to {PhoneNumber}.", phoneNumber);
                return false;
            }

            try
            {
                var payload = new Dictionary<string, string>
                {
                    ["To"] = phoneNumber,
                    ["From"] = _options.FromNumber,
                    ["Body"] = message
                };

                var sent = await RetryPolicyHelper.ExecuteExternalAsync(
                    () => SendViaProviderAsync(payload),
                    _logger);

                if (sent)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}.", phoneNumber);
                }
                else
                {
                    _logger.LogWarning("SMS provider returned non-success for {PhoneNumber}.", phoneNumber);
                }
                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}.", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendBulkSmsAsync(string[] phoneNumbers, string message)
        {
            if (phoneNumbers == null || phoneNumbers.Length == 0)
            {
                _logger.LogWarning("Bulk SMS send skipped: no phone numbers provided.");
                return false;
            }

            // Deduplicate and drop empties
            var uniqueNumbers = phoneNumbers
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToArray();

            if (uniqueNumbers.Length == 0)
            {
                _logger.LogWarning("Bulk SMS send skipped: no valid phone numbers provided.");
                return false;
            }

            // Send each independently so one failure doesn't abort the batch
            var tasks = uniqueNumbers.Select(async number =>
            {
                var ok = await SendSmsAsync(number, message);
                return (number, ok);
            });

            var results = await Task.WhenAll(tasks);

            var succeeded = results.Count(r => r.ok);
            var failed = results.Length - succeeded;

            _logger.LogInformation("Bulk SMS complete: {Succeeded}/{Total} sent, {Failed} failed.",
                succeeded, results.Length, failed);

            // Return true only if every recipient succeeded
            return failed == 0;
        }

        private async Task<bool> SendViaProviderAsync(Dictionary<string, string> payload)
        {
            var client = _httpClientFactory.CreateClient("SmsProvider");

            // Twilio-style Basic auth
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            var form = new FormUrlEncodedContent(payload);
            var response = await client.PostAsync("", form);
            return response.IsSuccessStatusCode;
        }
    }
}
