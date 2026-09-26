using SMS.Application.Features.OMS.Commands;
using SMS.Application.Features.OMS.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Common.Interfaces
{
    /// <summary>
    /// Shared delegation point for thin SMS module request adapters.
    ///
    /// An adapter's only job is to resolve module business context and hand a
    /// fully-formed <see cref="CreateRequestCommand"/> to this service, which
    /// performs the single authoritative Request creation path (RequestType
    /// validation, number generation, authorization, tenant stamping, audit,
    /// persistence). Adapters therefore never touch oms_requests directly and
    /// can never grow a second workflow engine.
    /// </summary>
    public interface IOmsRequestModuleAdapter
    {
        /// <summary>
        /// Creates an OMS Request through the generic command pipeline and
        /// returns the generic Request DTO.
        /// </summary>
        Task<RequestDto> CreateAsync(
            CreateRequestCommand command,
            CancellationToken cancellationToken = default);
    }
}