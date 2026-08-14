using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMS.Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Persistence.Repositories
{
    public class AssessmentRepository : BaseRepository<Assessment>, IAssessmentRepository
    {
        public AssessmentRepository(ApplicationDbContext context, ILogger<AssessmentRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<Assessment>> GetByUnitAsync(Guid unitId, CancellationToken ct = default)
            => await _dbSet.Where(a => a.UnitId == unitId && !a.IsDeleted).OrderBy(a => a.SortOrder).ToListAsync(ct);

        public async Task<IEnumerable<Assessment>> GetByCourseOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
            => await _dbSet.Where(a => a.CourseOfferingId == courseOfferingId && !a.IsDeleted).OrderBy(a => a.SortOrder).ToListAsync(ct);

        public async Task<IEnumerable<Assessment>> GetByLecturerAsync(Guid lecturerId, CancellationToken ct = default)
            => await _dbSet.Where(a => a.LecturerId == lecturerId && !a.IsDeleted).ToListAsync(ct);

        public async Task<decimal> GetTotalWeightForUnitAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var query = _dbSet.Where(a => a.UnitId == unitId && !a.IsDeleted && a.IsActive);
            if (courseOfferingId.HasValue)
                query = query.Where(a => a.CourseOfferingId == courseOfferingId);
            return await query.SumAsync(a => a.Weight, ct);
        }

        public async Task<bool> HasGradingStartedAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default)
        {
            var query = _dbSet.Where(a => a.UnitId == unitId && !a.IsDeleted);
            if (courseOfferingId.HasValue)
                query = query.Where(a => a.CourseOfferingId == courseOfferingId);
            return await query.AnyAsync(a => a.Status >= AssessmentStatus.GradingInProgress, ct);
        }

        public async Task<IEnumerable<Assessment>> GetBySemesterAsync(Guid semesterId, CancellationToken ct = default)
            => await _dbSet.Where(a => a.SemesterId == semesterId && !a.IsDeleted).ToListAsync(ct);
    }

    public class StudentAssessmentMarkRepository : BaseRepository<StudentAssessmentMark>, IStudentAssessmentMarkRepository
    {
        public StudentAssessmentMarkRepository(ApplicationDbContext context, ILogger<StudentAssessmentMarkRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<StudentAssessmentMark>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default)
            => await _dbSet.Where(m => m.AssessmentId == assessmentId && !m.IsDeleted).ToListAsync(ct);

        public async Task<IEnumerable<StudentAssessmentMark>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
            => await _dbSet.Where(m => m.StudentId == studentId && !m.IsDeleted).Include(m => m.Assessment).ToListAsync(ct);

        public async Task<StudentAssessmentMark?> GetByAssessmentAndStudentAsync(Guid assessmentId, Guid studentId, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(m => m.AssessmentId == assessmentId && m.StudentId == studentId && !m.IsDeleted, ct);

        public async Task<IEnumerable<StudentAssessmentMark>> GetByUnitAndStudentAsync(Guid unitId, Guid studentId, CancellationToken ct = default)
            => await _dbSet.Where(m => m.StudentId == studentId && !m.IsDeleted)
                .Include(m => m.Assessment)
                .Where(m => m.Assessment.UnitId == unitId)
                .ToListAsync(ct);

        public async Task<bool> ExistsAsync(Guid assessmentId, Guid studentId, CancellationToken ct = default)
            => await _dbSet.AnyAsync(m => m.AssessmentId == assessmentId && m.StudentId == studentId && !m.IsDeleted, ct);

        public async Task<int> CountGradedAsync(Guid assessmentId, CancellationToken ct = default)
            => await _dbSet.CountAsync(m => m.AssessmentId == assessmentId && !m.IsDeleted && !m.IsDraft, ct);

        public async Task<int> CountPendingGradingAsync(Guid assessmentId, CancellationToken ct = default)
            => await _dbSet.CountAsync(m => m.AssessmentId == assessmentId && !m.IsDeleted && m.IsDraft, ct);
    }

    public class AssessmentTypeRepository : BaseRepository<AssessmentType>, IAssessmentTypeRepository
    {
        public AssessmentTypeRepository(ApplicationDbContext context, ILogger<AssessmentTypeRepository> logger)
            : base(context, logger) { }

        public async Task<AssessmentType?> GetByCodeAsync(string code, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(t => t.Code == code && !t.IsDeleted, ct);

        public async Task<IEnumerable<AssessmentType>> GetActiveAsync(CancellationToken ct = default)
            => await _dbSet.Where(t => t.IsActive && !t.IsDeleted).OrderBy(t => t.SortOrder).ToListAsync(ct);
    }

    public class AssessmentTemplateRepository : BaseRepository<AssessmentTemplate>, IAssessmentTemplateRepository
    {
        public AssessmentTemplateRepository(ApplicationDbContext context, ILogger<AssessmentTemplateRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<AssessmentTemplate>> GetActiveAsync(CancellationToken ct = default)
            => await _dbSet.Where(t => t.IsActive && !t.IsDeleted).ToListAsync(ct);

        public async Task<IEnumerable<AssessmentTemplate>> GetByTypeAsync(Guid assessmentTypeId, CancellationToken ct = default)
            => await _dbSet.Where(t => t.AssessmentTypeId == assessmentTypeId && t.IsActive && !t.IsDeleted).ToListAsync(ct);
    }

    public class GradingScaleRepository : BaseRepository<GradingScale>, IGradingScaleRepository
    {
        public GradingScaleRepository(ApplicationDbContext context, ILogger<GradingScaleRepository> logger)
            : base(context, logger) { }

        public async Task<GradingScale?> GetDefaultAsync(CancellationToken ct = default)
            => await _dbSet.Include(s => s.Bands).FirstOrDefaultAsync(s => s.IsDefault && s.IsActive && !s.IsDeleted, ct);

        public async Task<GradingScale?> GetActiveVersionAsync(CancellationToken ct = default)
            => await _dbSet.Include(s => s.Bands)
                .FirstOrDefaultAsync(s => s.IsActive && !s.IsDeleted && (!s.EffectiveTo.HasValue || s.EffectiveTo >= DateTime.UtcNow), ct);

        public async Task<IEnumerable<GradingScale>> GetHistoryAsync(CancellationToken ct = default)
            => await _dbSet.Where(s => !s.IsDeleted).OrderByDescending(s => s.Version).ToListAsync(ct);
    }

    public class GradeBandRepository : BaseRepository<GradeBand>, IGradeBandRepository
    {
        public GradeBandRepository(ApplicationDbContext context, ILogger<GradeBandRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<GradeBand>> GetByScaleAsync(Guid gradingScaleId, CancellationToken ct = default)
            => await _dbSet.Where(b => b.GradingScaleId == gradingScaleId && !b.IsDeleted).OrderBy(b => b.SortOrder).ToListAsync(ct);
    }

    public class StudentCertificateEligibilityRepository : BaseRepository<StudentCertificateEligibility>, IStudentCertificateEligibilityRepository
    {
        public StudentCertificateEligibilityRepository(ApplicationDbContext context, ILogger<StudentCertificateEligibilityRepository> logger)
            : base(context, logger) { }

        public async Task<StudentCertificateEligibility?> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(e => e.StudentId == studentId && !e.IsDeleted, ct);

        public async Task<IEnumerable<StudentCertificateEligibility>> GetEligibleAsync(CancellationToken ct = default)
            => await _dbSet.Where(e => e.Status == CertificateEligibilityStatus.Eligible && !e.IsDeleted).ToListAsync(ct);

        public async Task<IEnumerable<StudentCertificateEligibility>> GetNotEligibleAsync(CancellationToken ct = default)
            => await _dbSet.Where(e => e.Status == CertificateEligibilityStatus.NotEligible && !e.IsDeleted).ToListAsync(ct);
    }

    public class GradeChangeHistoryRepository : BaseRepository<GradeChangeHistory>, IGradeChangeHistoryRepository
    {
        public GradeChangeHistoryRepository(ApplicationDbContext context, ILogger<GradeChangeHistoryRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<GradeChangeHistory>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
            => await _dbSet.Where(h => h.StudentId == studentId && !h.IsDeleted).OrderByDescending(h => h.ChangedDate).ToListAsync(ct);

        public async Task<IEnumerable<GradeChangeHistory>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default)
            => await _dbSet.Where(h => h.AssessmentId == assessmentId && !h.IsDeleted).OrderByDescending(h => h.ChangedDate).ToListAsync(ct);

        public async Task<IEnumerable<GradeChangeHistory>> GetByUnitAsync(Guid unitId, CancellationToken ct = default)
            => await _dbSet.Where(h => h.UnitId == unitId && !h.IsDeleted).OrderByDescending(h => h.ChangedDate).ToListAsync(ct);
    }

    public class UnitResultRepository : BaseRepository<UnitResult>, IUnitResultRepository
    {
        public UnitResultRepository(ApplicationDbContext context, ILogger<UnitResultRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<UnitResult>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
            => await _dbSet.Where(r => r.StudentId == studentId && !r.IsDeleted).Include(r => r.Unit).ToListAsync(ct);

        public async Task<IEnumerable<UnitResult>> GetByUnitAsync(Guid unitId, CancellationToken ct = default)
            => await _dbSet.Where(r => r.UnitId == unitId && !r.IsDeleted).Include(r => r.Student).ToListAsync(ct);

        public async Task<UnitResult?> GetByStudentAndUnitAsync(Guid studentId, Guid unitId, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(r => r.StudentId == studentId && r.UnitId == unitId && !r.IsDeleted, ct);

        public async Task<IEnumerable<UnitResult>> GetByCourseOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
            => await _dbSet.Where(r => r.CourseOfferingId == courseOfferingId && !r.IsDeleted).Include(r => r.Student).ToListAsync(ct);

        public async Task<IEnumerable<UnitResult>> GetPublishedByStudentAsync(Guid studentId, CancellationToken ct = default)
            => await _dbSet.Where(r => r.StudentId == studentId && r.IsPublished && !r.IsDeleted).Include(r => r.Unit).ToListAsync(ct);

        public async Task<IEnumerable<UnitResult>> GetByStatusAsync(ResultPublicationStatus status, CancellationToken ct = default)
            => await _dbSet.Where(r => r.PublicationStatus == status && !r.IsDeleted).Include(r => r.Student).Include(r => r.Unit).ToListAsync(ct);
    }

    public class ModerationRecordRepository : BaseRepository<ModerationRecord>, IModerationRecordRepository
    {
        public ModerationRecordRepository(ApplicationDbContext context, ILogger<ModerationRecordRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<ModerationRecord>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default)
            => await _dbSet.Where(r => r.AssessmentId == assessmentId && !r.IsDeleted).OrderByDescending(r => r.ModeratedDate).ToListAsync(ct);

        public async Task<IEnumerable<ModerationRecord>> GetPendingAsync(CancellationToken ct = default)
            => await _dbSet.Where(r => r.Status == ModerationStatus.PendingReview && !r.IsDeleted).ToListAsync(ct);

        public async Task<IEnumerable<ModerationRecord>> GetByModeratorAsync(string moderatorId, CancellationToken ct = default)
            => await _dbSet.Where(r => r.ModeratedBy == moderatorId && !r.IsDeleted).ToListAsync(ct);
    }

    public class AssessmentExemptionRepository : BaseRepository<AssessmentExemption>, IAssessmentExemptionRepository
    {
        public AssessmentExemptionRepository(ApplicationDbContext context, ILogger<AssessmentExemptionRepository> logger)
            : base(context, logger) { }

        public async Task<IEnumerable<AssessmentExemption>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default)
            => await _dbSet.Where(e => e.AssessmentId == assessmentId && e.IsActive && !e.IsDeleted).ToListAsync(ct);

        public async Task<IEnumerable<AssessmentExemption>> GetByStudentAsync(Guid studentId, CancellationToken ct = default)
            => await _dbSet.Where(e => e.StudentId == studentId && e.IsActive && !e.IsDeleted).ToListAsync(ct);

        public async Task<AssessmentExemption?> GetByAssessmentAndStudentAsync(Guid assessmentId, Guid studentId, CancellationToken ct = default)
            => await _dbSet.FirstOrDefaultAsync(e => e.AssessmentId == assessmentId && e.StudentId == studentId && e.IsActive && !e.IsDeleted, ct);
    }
}
