using System.Threading.Tasks;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Generates unique, collision-free usernames from a user's first and last
    /// name using a priority-based algorithm. The generator checks each
    /// candidate against existing users before proposing it.
    /// </summary>
    public interface IUsernameGenerator
    {
        /// <summary>
        /// Validates that a candidate username is well-formed (lowercase
        /// letters/numbers only, within max length) and not already in use.
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <returns>True if the username is available and valid.</returns>
        Task<bool> IsUsernameAvailableAsync(string username);

        /// <summary>
        /// Generates a unique username from the given names, trying candidate
        /// variants in priority order and appending an incrementing number if
        /// all simple variants are taken.
        /// </summary>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <returns>A guaranteed-unique, valid username.</returns>
        Task<string> GenerateUsernameAsync(string firstName, string lastName);
    }
}
