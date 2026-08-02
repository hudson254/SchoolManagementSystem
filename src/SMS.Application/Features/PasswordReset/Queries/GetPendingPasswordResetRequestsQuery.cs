using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Application.Features.PasswordReset.Queries
{
    public class GetPendingPasswordResetRequestsQuery : IRequest<List<PasswordResetRequest>>
    {
    }

    public class GetPendingPasswordResetRequestsQueryHandler : IRequestHandler<GetPendingPasswordResetRequestsQuery, List<PasswordResetRequest>>
    {
        private readonly IPasswordResetRequestRepository _repository;

        public GetPendingPasswordResetRequestsQueryHandler(IPasswordResetRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PasswordResetRequest>> Handle(GetPendingPasswordResetRequestsQuery request, CancellationToken cancellationToken)
        {
            var pending = await _repository.GetPendingAsync();
            return pending.ToList();
        }
    }
}
