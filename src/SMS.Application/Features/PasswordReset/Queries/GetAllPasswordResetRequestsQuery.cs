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
    public class GetAllPasswordResetRequestsQuery : IRequest<List<PasswordResetRequest>>
    {
        public PasswordResetRequestStatus? StatusFilter { get; set; }
    }

    public class GetAllPasswordResetRequestsQueryHandler : IRequestHandler<GetAllPasswordResetRequestsQuery, List<PasswordResetRequest>>
    {
        private readonly IPasswordResetRequestRepository _repository;

        public GetAllPasswordResetRequestsQueryHandler(IPasswordResetRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PasswordResetRequest>> Handle(GetAllPasswordResetRequestsQuery request, CancellationToken cancellationToken)
        {
            if (request.StatusFilter.HasValue)
            {
                return await Task.FromResult(_repository.GetPendingAsync().Result.ToList());
            }

            var all = await _repository.GetAllAsync();
            return all.ToList();
        }
    }
}
