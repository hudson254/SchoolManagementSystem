using MediatR;
using SMS.Application.Common.Interfaces;
using SMS.Application.Features.OMS.Commands;
using SMS.Application.Features.OMS.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Application.Features.OMS.Services
{
    /// <summary>
    /// Default <see cref="IOmsRequestModuleAdapter"/>: dispatches the generic
    /// <see cref="CreateRequestCommand"/> through MediatR so the adapter runs
    /// the same pipeline behaviours (validation, logging) and the same
    /// handler-level authorization, tenant stamping, RequestType validation,
    /// number generation and audit as the public generic endpoint.
    ///
    /// There is deliberately no alternate persistence path here: an adapter
    /// that bypassed this class would be creating a second workflow engine.
    /// </summary>
    public class OmsRequestModuleAdapter : IOmsRequestModuleAdapter
    {
        private readonly IMediator _mediator;

        public OmsRequestModuleAdapter(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task<RequestDto> CreateAsync(
            CreateRequestCommand command,
            CancellationToken cancellationToken = default) =>
            _mediator.Send(command, cancellationToken);
    }
}