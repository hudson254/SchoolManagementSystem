namespace SMS.Domain.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task SendEmailAsync(string to, string subject, string body, string? from = null, bool isHtml = false);
        Task SendWelcomeEmailAsync(string to, string name);
        Task SendVerificationEmailAsync(string to, string name, string token, Guid userId);
        Task SendPasswordResetEmailAsync(string to, string name, string token);
        Task SendAssignmentNotificationAsync(string to, string name, string assignmentTitle);
        Task SendGradeNotificationAsync(string to, string name, string unitName, string grade);
    }
}