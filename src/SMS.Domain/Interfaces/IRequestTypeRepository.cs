using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Repository for OMS request types.
    /// </summary>
    public interface IRequestTypeRepository : IRepository<RequestType>
    {
        Task<RequestType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RequestType>> GetActiveAsync(CancellationToken cancellationToken = default);
    }
}
