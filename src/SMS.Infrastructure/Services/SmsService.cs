using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;
using System;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
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
            try
            {
                _logger.LogInformation("Sending SMS to {PhoneNumber}: {Message}", phoneNumber, message);

                // In a real implementation, you'd use Twilio or another SMS provider
                // For now, we'll just log it as acknowledged

                _logger.LogInformation("SMS acknowledged - would send to {PhoneNumber}: {Message}", phoneNumber, message);
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
            try
            {
                foreach (var phoneNumber in phoneNumbers)
                {
                    await SendSmsAsync(phoneNumber, message);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk SMS");
                return false;
            }
        }
    }
}
