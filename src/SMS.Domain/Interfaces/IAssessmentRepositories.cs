using SMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SMS.Domain.Interfaces
{
    public interface IAssessmentRepository : IRepository<Assessment>
    {
        Task<IEnumerable<Assessment>> GetByUnitAsync(Guid unitId, CancellationToken ct = default);
        Task<IEnumerable<Assessment>> GetByCourseOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);
        Task<IEnumerable<Assessment>> GetByLecturerAsync(Guid lecturerId, CancellationToken ct = default);
        Task<decimal> GetTotalWeightForUnitAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default);
        Task<bool> HasGradingStartedAsync(Guid unitId, Guid? courseOfferingId, CancellationToken ct = default);
        Task<IEnumerable<Assessment>> GetBySemesterAsync(Guid semesterId, CancellationToken ct = default);
    }

    public interface IStudentAssessmentMarkRepository : IRepository<StudentAssessmentMark>
    {
        Task<IEnumerable<StudentAssessmentMark>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default);
        Task<IEnumerable<StudentAssessmentMark>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
        Task<StudentAssessmentMark?> GetByAssessmentAndStudentAsync(Guid assessmentId, Guid studentId, CancellationToken ct = default);
        Task<IEnumerable<StudentAssessmentMark>> GetByUnitAndStudentAsync(Guid unitId, Guid studentId, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid assessmentId, Guid studentId, CancellationToken ct = default);
        Task<int> CountGradedAsync(Guid assessmentId, CancellationToken ct = default);
        Task<int> CountPendingGradingAsync(Guid assessmentId, CancellationToken ct = default);
    }

    public interface IAssessmentTypeRepository : IRepository<AssessmentType>
    {
        Task<AssessmentType?> GetByCodeAsync(string code, CancellationToken ct = default);
        Task<IEnumerable<AssessmentType>> GetActiveAsync(CancellationToken ct = default);
    }

    public interface IAssessmentTemplateRepository : IRepository<AssessmentTemplate>
    {
        Task<IEnumerable<AssessmentTemplate>> GetActiveAsync(CancellationToken ct = default);
        Task<IEnumerable<AssessmentTemplate>> GetByTypeAsync(Guid assessmentTypeId, CancellationToken ct = default);
    }

    public interface IGradingScaleRepository : IRepository<GradingScale>
    {
        Task<GradingScale?> GetDefaultAsync(CancellationToken ct = default);
        Task<GradingScale?> GetActiveVersionAsync(CancellationToken ct = default);
        Task<IEnumerable<GradingScale>> GetHistoryAsync(CancellationToken ct = default);
    }

    public interface IGradeBandRepository : IRepository<GradeBand>
    {
        Task<IEnumerable<GradeBand>> GetByScaleAsync(Guid gradingScaleId, CancellationToken ct = default);
    }

    public interface IStudentCertificateEligibilityRepository : IRepository<StudentCertificateEligibility>
    {
        Task<StudentCertificateEligibility?> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
        Task<IEnumerable<StudentCertificateEligibility>> GetEligibleAsync(CancellationToken ct = default);
        Task<IEnumerable<StudentCertificateEligibility>> GetNotEligibleAsync(CancellationToken ct = default);
    }

    public interface IGradeChangeHistoryRepository : IRepository<GradeChangeHistory>
    {
        Task<IEnumerable<GradeChangeHistory>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
        Task<IEnumerable<GradeChangeHistory>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default);
        Task<IEnumerable<GradeChangeHistory>> GetByUnitAsync(Guid unitId, CancellationToken ct = default);
    }

    public interface IUnitResultRepository : IRepository<UnitResult>
    {
        Task<IEnumerable<UnitResult>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
        Task<IEnumerable<UnitResult>> GetByUnitAsync(Guid unitId, CancellationToken ct = default);
        Task<UnitResult?> GetByStudentAndUnitAsync(Guid studentId, Guid unitId, CancellationToken ct = default);
        Task<IEnumerable<UnitResult>> GetByCourseOfferingAsync(Guid courseOfferingId, CancellationToken ct = default);
        Task<IEnumerable<UnitResult>> GetPublishedByStudentAsync(Guid studentId, CancellationToken ct = default);
        Task<IEnumerable<UnitResult>> GetByStatusAsync(SMS.Domain.Enums.ResultPublicationStatus status, CancellationToken ct = default);
    }

    public interface IModerationRecordRepository : IRepository<ModerationRecord>
    {
        Task<IEnumerable<ModerationRecord>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default);
        Task<IEnumerable<ModerationRecord>> GetPendingAsync(CancellationToken ct = default);
        Task<IEnumerable<ModerationRecord>> GetByModeratorAsync(string moderatorId, CancellationToken ct = default);
    }

    public interface IAssessmentExemptionRepository : IRepository<AssessmentExemption>
    {
        Task<IEnumerable<AssessmentExemption>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default);
        Task<IEnumerable<AssessmentExemption>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
        Task<AssessmentExemption?> GetByAssessmentAndStudentAsync(Guid assessmentId, Guid studentId, CancellationToken ct = default);
    }
}
