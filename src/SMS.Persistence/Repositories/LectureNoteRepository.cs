using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;
using SMS.Persistence.Data;

namespace SMS.Persistence.Repositories
{
    public class LectureNoteRepository : BaseRepository<LectureNote>, ILectureNoteRepository
    {
        public LectureNoteRepository(ApplicationDbContext context, ILogger<LectureNoteRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<LectureNote>> GetByUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.UnitId == unitId && !n.IsDeleted)
                .Include(n => n.Lecturer)
                .Include(n => n.UploadFile)
                .OrderByDescending(n => n.UploadDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<LectureNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(n => n.Lecturer)
                .Include(n => n.Unit)
                .Include(n => n.UploadFile)
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsByUploadFileAsync(Guid uploadFileId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(n => n.UploadFileId == uploadFileId && !n.IsDeleted, cancellationToken);
        }
    }
}