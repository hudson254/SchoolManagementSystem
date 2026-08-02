using SMS.Domain.Interfaces;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Re-exports the domain ICurrentUserService for Application layer use.
    /// </summary>
    public interface ICurrentUserService : SMS.Domain.Interfaces.ICurrentUserService
    {
    }
}
