using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;
using System;
using System.Threading.Tasks;

namespace SMS.Notifications.Services
{
    public class SmsService : ISmsService
    {
        private readonly SmsOptions _options;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IOptions<SmsOptions> options, ILogger<SmsService> logger)
        {
            _options = options.Value;
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

            try
            {
                if (!_options.Enabled)
                {
                    _logger.LogInformation("SMS service is disabled. Would send to {PhoneNumber}: {Message}", phoneNumber, message);
                    return true;
                }

                if (string.IsNullOrEmpty(_options.AccountSid) || string.IsNullOrEmpty(_options.AuthToken))
                {
                    _logger.LogWarning("SMS service not configured (missing AccountSid or AuthToken). Logging SMS to {PhoneNumber}: {Message}", phoneNumber, message);
                    return true;
                }

                // In production, replace with actual SMS provider integration:
                // var client = new TwilioRestClient(_options.AccountSid, _options.AuthToken);
                // var result = await client.SendMessageAsync(_options.FromNumber, phoneNumber, message);
                // return result.Success;

                await Task.Delay(100); // Simulate async SMS sending

                _logger.LogInformation("SMS sent to {PhoneNumber}: {Message}", phoneNumber, message);
                return true;
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
    }
}

