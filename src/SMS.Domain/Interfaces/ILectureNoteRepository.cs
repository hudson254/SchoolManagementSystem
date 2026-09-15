using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    /// <summary>
    /// Repository for study material (lecture note) domain records.
    /// </summary>
    public interface ILectureNoteRepository : IRepository<LectureNote>
    {
        Task<IEnumerable<LectureNote>> GetByUnitAsync(Guid unitId, CancellationToken cancellationToken = default);
        Task<LectureNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByUploadFileAsync(Guid uploadFileId, CancellationToken cancellationToken = default);
    }
}