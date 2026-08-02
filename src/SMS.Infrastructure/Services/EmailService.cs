using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SMS.Domain.Interfaces;
using SMS.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            await SendEmailAsync(toEmail, subject, body, (List<string>)null);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, List<string> attachments)
        {
            try
            {
                using (var client = new SmtpClient(_options.Host, _options.Port))
                {
                    client.EnableSsl = _options.EnableSsl;
                    client.Credentials = new NetworkCredential(_options.Username, _options.Password);
                    client.Timeout = 30000;

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_options.From, _options.FromName);
                        message.To.Add(toEmail);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        if (attachments != null)
                        {
                            foreach (var attachment in attachments)
                            {
                                var attachmentItem = new Attachment(attachment);
                                message.Attachments.Add(attachmentItem);
                            }
                        }

                        await client.SendMailAsync(message);
                        _logger.LogInformation("Email sent to {ToEmail}", toEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentName)
        {
            try
            {
                using (var client = new SmtpClient(_options.Host, _options.Port))
                {
                    client.EnableSsl = _options.EnableSsl;
                    client.Credentials = new NetworkCredential(_options.Username, _options.Password);

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_options.From, _options.FromName);
                        message.To.Add(toEmail);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        if (attachmentData != null && !string.IsNullOrEmpty(attachmentName))
                        {
                            var attachment = new Attachment(new System.IO.MemoryStream(attachmentData), attachmentName);
                            message.Attachments.Add(attachment);
                        }

                        await client.SendMailAsync(message);
                        _logger.LogInformation("Email with attachment sent to {ToEmail}", toEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with attachment to {ToEmail}", toEmail);
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Password Reset Request";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>You have requested to reset your password. Click the link below to reset your password:</p>
                <p><a href='{resetLink}'>{resetLink}</a></p>
                <p>This link will expire in 24 hours.</p>
                <p>If you did not request this, please ignore this email.</p>
            ";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
        {
            var subject = "Verify Your Email Address";
            var body = $@"
                <h2>Email Verification</h2>
                <p>Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}'>{verificationLink}</a></p>
                <p>This link will expire in 24 hours.</p>
            ";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendTemplateEmailAsync(string toEmail, string templateName, Dictionary<string, string> templateData)
        {
            var subject = $"Template: {templateName}";
            var body = $"<h2>{templateName}</h2>";
            foreach (var data in templateData)
            {
                body += $"<p><strong>{data.Key}:</strong> {data.Value}</p>";
            }
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailToMultipleAsync(List<string> toEmails, string subject, string body)
        {
            foreach (var email in toEmails)
            {
                await SendEmailAsync(email, subject, body);
            }
        }
    }
}
