using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SMS.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly SmsOptions _options;
        private readonly ILogger<SmsService> _logger;

        public SmsService(
            IOptions<SmsOptions> options,
            ILogger<SmsService> logger)
        {
            _options = options.Value;
            _logger = logger;

            TwilioClient.Init(_options.AccountSid, _options.AuthToken);
        }

        public async Task SendSmsAsync(string to, string message)
        {
            try
            {
                var result = await MessageResource.CreateAsync(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(_options.From),
                    to: new Twilio.Types.PhoneNumber(to)
                );

                _logger.LogInformation("SMS sent to {To}, SID: {Sid}", to, result.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {To}", to);
                throw;
            }
        }

        public async Task SendSmsAsync(string to, string message, string from)
        {
            try
            {
                var result = await MessageResource.CreateAsync(
                    body: message,
                    from: new Twilio.Types.PhoneNumber(from),
                    to: new Twilio.Types.PhoneNumber(to)
                );

                _logger.LogInformation("SMS sent to {To} from {From}, SID: {Sid}", to, from, result.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {To}", to);
                throw;
            }
        }

        public async Task SendAttendanceNotificationAsync(string to, string studentName, string className, string status)
        {
            var message = $"Dear {studentName}, your attendance for {className} has been recorded as {status}. Date: {DateTime.UtcNow:yyyy-MM-dd}";
            await SendSmsAsync(to, message);
        }

        public async Task SendAssignmentNotificationAsync(string to, string studentName, string assignmentTitle, DateTime dueDate)
        {
            var message = $"Dear {studentName}, assignment '{assignmentTitle}' is due on {dueDate:yyyy-MM-dd HH:mm}. Please submit before the deadline.";
            await SendSmsAsync(to, message);
        }

        public async Task SendGradeNotificationAsync(string to, string studentName, string unitName, string grade)
        {
            var message = $"Dear {studentName}, your grade for {unitName} has been posted: {grade}";
            await SendSmsAsync(to, message);
        }
    }
}