using System.Collections.Generic;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailAsync(string toEmail, string subject, string body, List<string> attachments);
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentData, string attachmentName);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendVerificationEmailAsync(string toEmail, string verificationLink);
        Task SendTemplateEmailAsync(string toEmail, string templateName, Dictionary<string, string> templateData);
        Task SendEmailToMultipleAsync(List<string> toEmails, string subject, string body);
    }
}