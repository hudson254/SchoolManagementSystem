using SMS.Domain.Entities;
using SMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// SMS request repository.
    /// </summary>
    public interface IRequestRepository : IRepository<Request>
    {
        Task<Request?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Request?> GetByRequestNumberAsync(string requestNumber, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RequestStatusHistory>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RequestComment>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
            RequestStatus? status,
            string? requestType,
            string? requesterUserId,
            string? assignedUserId,
            string? search,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<RequestStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Request>> GetByRequesterUserIdAsync(string requesterUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Request>> GetByAssignedUserIdAsync(string assignedUserId, CancellationToken cancellationToken = default);

        /// <summary>Get all request types for the given tenant.</summary>
        Task<IReadOnlyList<RequestType>> GetAllTypesAsync(CancellationToken cancellationToken = default);

        /// <summary>Check whether a request type with the given code already exists in the tenant.</summary>
        Task<bool> ExistsByTypeCodeAsync(string code, Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>Get a request type by its identifier.</summary>
        Task<RequestType?> GetRequestTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Get a request type by its code.</summary>
        Task<RequestType?> GetByTypeCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>Add a request type.</summary>
        Task AddRequestTypeAsync(RequestType type, CancellationToken cancellationToken = default);

        /// <summary>Update a request type.</summary>
        Task UpdateRequestTypeAsync(RequestType type, CancellationToken cancellationToken = default);

        /// <summary>Get a request attachment by its identifier (tenant-scoped).</summary>
        Task<RequestAttachment?> GetAttachmentByIdAsync(Guid attachmentId);

        /// <summary>List attachments for a request (tenant-scoped).</summary>
        Task<IReadOnlyList<RequestAttachment>> GetAttachmentsByRequestIdAsync(Guid requestId);

        /// <summary>Add a request attachment association.</summary>
        Task AddAttachmentAsync(RequestAttachment attachment);

        /// <summary>Remove a request attachment association.</summary>
        void RemoveAttachment(RequestAttachment attachment);
    }
}
