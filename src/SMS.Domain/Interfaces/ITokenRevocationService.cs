using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Service for revoking access tokens (short-lived deny-list).
    /// On a single-instance LAN deployment an in-memory implementation
    /// is sufficient. If multi-instance scaling is added later, replace
    /// with a distributed cache (e.g. Redis) implementation.
    /// </summary>
    public interface ITokenRevocationService
    {
        /// <summary>
        /// Adds a JWT identifier (jti claim) to the deny-list so the
        /// token can no longer be used even before its natural expiry.
        /// </summary>
        Task RevokeAccessTokenAsync(string jti);

        /// <summary>
        /// Returns true if the given JWT identifier has been revoked.
        /// </summary>
        Task<bool> IsAccessTokenRevokedAsync(string jti);
    }
}
