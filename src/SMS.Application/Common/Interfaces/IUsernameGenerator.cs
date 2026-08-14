using System.Threading.Tasks;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Generates unique, collision-free usernames. Usernames must never
    /// include professional or academic titles.
    /// </summary>
    public interface IUsernameGenerator
    {
        /// <summary>
        /// Checks if a username is available (unique) in the system.
        /// </summary>
        Task<bool> IsUsernameAvailableAsync(string username);

        /// <summary>
        /// Generates a unique username from first and last name.
        /// Titles are never included in the generated username.
        /// </summary>
        Task<string> GenerateUsernameAsync(string firstName, string lastName);

        /// <summary>
        /// Generates a unique username from a full name string.
        /// The name is parsed to extract and remove any titles before
        /// generating the username.
        /// </summary>
        Task<string> GenerateUsernameFromFullNameAsync(string fullName);
    }
}
