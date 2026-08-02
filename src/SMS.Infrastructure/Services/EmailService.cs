using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;

namespace SMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailOptions> options,
            ILogger<EmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            await SendEmailAsync(to, subject, body, null, isHtml);
        }

        public async Task SendEmailAsync(string to, string subject, string body, string? from = null, bool isHtml = false)
        {
            try
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    Credentials = new NetworkCredential(_options.Username, _options.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(from ?? _options.From),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string to, string name)
        {
            var subject = "Welcome to the School Management System";
            var body = $@"
                <h2>Welcome {name}!</h2>
                <p>Your account has been created successfully. You can now log in to the School Management System.</p>
                <p><strong>Login Credentials:</strong></p>
                <p>Email: {to}</p>
                <p>Password: Please use the password you set during registration.</p>
                <p><a href='https://localhost/login'>Click here to log in</a></p>
                <br/>
                <p>Thank you,</p>
                <p>School Management System Team</p>
            ";

            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendVerificationEmailAsync(string to, string name, string token, Guid userId)
        {
            var verificationLink = $"https://localhost/api/auth/verify-email?userId={userId}&token={Uri.EscapeDataString(token)}";
            var subject = "Verify Your Email Address";
            var body = $@"
                <h2>Hello {name}!</h2>
                <p>Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}'>Verify Email Address</a></p>
                <p>If you did not create an account, please ignore this email.</p>
                <br/>
                <p>Thank you,</p>
                <p>School Management System Team</p>
            ";

            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendPasswordResetEmailAsync(string to, string name, string token)
        {
            var resetLink = $"https://localhost/reset-password?email={Uri.EscapeDataString(to)}&token={Uri.EscapeDataString(token)}";
            var subject = "Reset Your Password";
            var body = $@"
                <h2>Hello {name}!</h2>
                <p>You have requested to reset your password. Click the link below to set a new password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>This link will expire in 24 hours.</p>
                <p>If you did not request a password reset, please ignore this email.</p>
                <br/>
                <p>Thank you,</p>
                <p>School Management System Team</p>
            ";

            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendAssignmentNotificationAsync(string to, string name, string assignmentTitle)
        {
            var subject = $"New Assignment: {assignmentTitle}";
            var body = $@"
                <h2>Hello {name}!</h2>
                <p>A new assignment has been published: <strong>{assignmentTitle}</strong></p>
                <p>Please log in to the School Management System to view and submit the assignment.</p>
                <p><a href='https://localhost/assignments'>View Assignments</a></p>
                <br/>
                <p>Thank you,</p>
                <p>School Management System Team</p>
            ";

            await SendEmailAsync(to, subject, body, true);
        }

        public async Task SendGradeNotificationAsync(string to, string name, string unitName, string grade)
        {
            var subject = $"Grade Posted: {unitName}";
            var body = $@"
                <h2>Hello {name}!</h2>
                <p>Your grade for <strong>{unitName}</strong> has been posted.</p>
                <p><strong>Grade:</strong> {grade}</p>
                <p>Please log in to the School Management System to view your full results.</p>
                <p><a href='https://localhost/grades'>View Grades</a></p>
                <br/>
                <p>Thank you,</p>
                <p>School Management System Team</p>
            ";

            await SendEmailAsync(to, subject, body, true);
        }
    }
}