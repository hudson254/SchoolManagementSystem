using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendBulkSmsAsync(string[] phoneNumbers, string message);
    }
}