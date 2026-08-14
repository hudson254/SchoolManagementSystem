# Assessment Engine - Domain Layer Generation Script
# Creates all domain entities, enums, and interfaces for the assessment system

$root = "src\SMS.Domain"

# ============================================================
# ENUMS
# ============================================================

# CertificateEligibilityStatus
@'
namespace SMS.Domain.Enums
{
    public enum CertificateEligibilityStatus
    {
        NotDetermined = 0,
        Eligible = 1,
        NotEligible = 2,
        PendingCompletion = 3
    }
}
'@ | Set-Content -Path "$root\Enums\CertificateEligibilityStatus.cs" -Encoding UTF8

# GradeChangeReason
@'
namespace SMS.Domain.Enums
{
    public enum GradeChangeReason
    {
        Correction = 1,
        Appeal = 2,
        Recalculation = 3,
        Administrative = 4,
        Other = 5
    }
}
'@ | Set-Content -Path "$root\Enums\GradeChangeReason.cs" -Encoding UTF8

# MarkEntrySource
@'
namespace SMS.Domain.Enums
{
    public enum MarkEntrySource
    {
        ManualEntry = 1,
        BulkImport = 2,
        OnlineAssessment = 3,
        SystemCalculated = 4
    }
}
'@ | Set-Content -Path "$root\Enums\MarkEntrySource.cs" -Encoding UTF8

# AssessmentVisibility
@'
namespace SMS.Domain.Enums
{
    public enum AssessmentVisibility
    {
        Hidden = 0,
        VisibleToStudents = 1,
        VisibleToStaff = 2,
        Public = 3
    }
}
'@ | Set-Content -Path "$root\Enums\AssessmentVisibility.cs" -Encoding UTF8

# ============================================================
# ENTITIES
# ============================================================

# StudentAssessmentMark
@'
using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Tracks a student's mark for a specific assessment.
    /// </summary>
    public class StudentAssessmentMark : BaseEntity, ITenantAwareEntity
    {
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? CourseOfferingId { get; set; }

        /// <summary>Raw mark before weighting (0-MaxScore).</summary>
        public decimal Mark { get; set; }

        /// <summary>0-100 normalized percentage.</summary>
        public decimal Percentage { get; set; }

        /// <summary>Weighted contribution to final score (Percentage * Weight / 100).</summary>
        public decimal WeightedScore { get; set; }

        public bool IsDraft { get; set; } = true;
        public bool IsModerated { get; set; }
        public DateTime? ModeratedDate { get; set; }
        public string? ModeratedBy { get; set; }
        public string? ModerationComment { get; set; }

        public decimal? OriginalMark { get; set; }
        public decimal? RevisedMark { get; set; }

        public MarkEntrySource EntrySource { get; set; } = MarkEntrySource.ManualEntry;
        public string? ImportBatchReference { get; set; }

        public bool IsExempt { get; set; }
        public string? ExemptionReason { get; set; }

        public string? Feedback { get; set; }
        public bool FeedbackPublished { get; set; }

        public Guid? GradedBy { get; set; }
        public DateTime? GradedDate { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
        public virtual Enrollment Enrollment { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\StudentAssessmentMark.cs" -Encoding UTF8

# AssessmentExemption
@'
using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Records an exemption from a specific assessment for a student.
    /// </summary>
    public class AssessmentExemption : BaseEntity, ITenantAwareEntity
    {
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? GrantedBy { get; set; }
        public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual Student Student { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\AssessmentExemption.cs" -Encoding UTF8

# AssessmentTemplate
@'
using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Reusable assessment configuration template that can be applied to multiple units.
    /// </summary>
    public class AssessmentTemplate : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public Guid? AssessmentTypeId { get; set; }
        public bool IsMandatory { get; set; }
        public bool RequiresModeration { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SortOrder { get; set; }

        public virtual AssessmentType AssessmentType { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\AssessmentTemplate.cs" -Encoding UTF8

# GradingScale
@'
using SMS.Domain.Common;
using System;
using System.Collections.Generic;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Versioned grading scale configuration. Historical results retain the
    /// grading scale version that was in effect at the time of publication.
    /// </summary>
    public class GradingScale : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? CreatedBy { get; set; }

        public virtual ICollection<GradeBand> Bands { get; set; } = new List<GradeBand>();
    }
}
'@ | Set-Content -Path "$root\Entities\GradingScale.cs" -Encoding UTF8

# GradeBand
@'
using SMS.Domain.Common;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// A single grade band within a grading scale.
    /// </summary>
    public class GradeBand : BaseEntity, ITenantAwareEntity
    {
        public Guid GradingScaleId { get; set; }
        public decimal MinPercentage { get; set; }
        public decimal MaxPercentage { get; set; }
        public string GradeLetter { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? GpaPoints { get; set; }
        public string ColorCode { get; set; } = "#000000";
        public string? HonorsClassification { get; set; }
        public int SortOrder { get; set; }

        public virtual GradingScale GradingScale { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\GradeBand.cs" -Encoding UTF8

# CertificateRule
@'
using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Institution-wide rules for determining certificate eligibility.
    /// </summary>
    public class CertificateRule : BaseEntity, ITenantAwareEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? MinimumPassingPercentage { get; set; }
        public string? MinimumPassingGradeLetter { get; set; }
        public bool RequireAllMandatoryAssessments { get; set; }
        public bool RequireNoOutstandingIncomplete { get; set; }
        public bool RequireAllRequiredUnits { get; set; }
        public string? AdditionalRequirements { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVersioned { get; set; } = true;
        public int Version { get; set; } = 1;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? CreatedBy { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\CertificateRule.cs" -Encoding UTF8

# StudentCertificateEligibility
@'
using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Tracks the certificate eligibility status for a student.
    /// </summary>
    public class StudentCertificateEligibility : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }
        public Guid? CertificateRuleId { get; set; }
        public CertificateEligibilityStatus Status { get; set; } = CertificateEligibilityStatus.NotDetermined;
        public decimal? OverallPercentage { get; set; }
        public string? OverallGradeLetter { get; set; }
        public bool HasOutstandingIncomplete { get; set; }
        public bool HasFailedRequiredUnits { get; set; }
        public string? EligibilityDetails { get; set; }
        public DateTime? EvaluatedDate { get; set; }
        public string? EvaluatedBy { get; set; }

        public virtual Student Student { get; set; }
        public virtual CertificateRule CertificateRule { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\StudentCertificateEligibility.cs" -Encoding UTF8

# GradeChangeHistory
@'
using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Immutable record of every grade change event.
    /// </summary>
    public class GradeChangeHistory : BaseEntity, ITenantAwareEntity
    {
        public Guid? AssessmentId { get; set; }
        public Guid? StudentAssessmentMarkId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? CourseOfferingId { get; set; }

        public decimal PreviousScore { get; set; }
        public decimal NewScore { get; set; }
        public string? PreviousGradeLetter { get; set; }
        public string? NewGradeLetter { get; set; }

        public GradeChangeReason ChangeReason { get; set; }
        public string? Reason { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
        public string? ChangeDetailsJson { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual StudentAssessmentMark StudentAssessmentMark { get; set; }
        public virtual Student Student { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\GradeChangeHistory.cs" -Encoding UTF8

# UnitResult
@'
using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Final computed result for a student in a unit.
    /// </summary>
    public class UnitResult : BaseEntity, ITenantAwareEntity
    {
        public Guid StudentId { get; set; }
        public Guid UnitId { get; set; }
        public Guid? EnrollmentId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid? GradingScaleVersionId { get; set; }

        public decimal FinalPercentage { get; set; }
        public string GradeLetter { get; set; } = string.Empty;
        public string GradeDescription { get; set; } = string.Empty;
        public decimal? GpaPoints { get; set; }

        public ResultPublicationStatus PublicationStatus { get; set; } = ResultPublicationStatus.Draft;
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.NotRequired;

        public bool IsPublished { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? PublishedBy { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? ApprovedBy { get; set; }

        public bool IsRecalculated { get; set; }
        public DateTime? LastCalculatedDate { get; set; }
        public string? LastCalculatedBy { get; set; }

        public virtual Student Student { get; set; }
        public virtual Unit Unit { get; set; }
        public virtual Enrollment Enrollment { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual GradingScale GradingScaleVersion { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\UnitResult.cs" -Encoding UTF8

# ModerationRecord
@'
using SMS.Domain.Common;
using SMS.Domain.Enums;
using System;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// Records a moderation review of assessment marks.
    /// </summary>
    public class ModerationRecord : BaseEntity, ITenantAwareEntity
    {
        public Guid AssessmentId { get; set; }
        public Guid? CourseOfferingId { get; set; }
        public Guid? UnitId { get; set; }
        public ModerationStatus Status { get; set; } = ModerationStatus.PendingReview;
        public string? Comments { get; set; }
        public string? ModeratedBy { get; set; }
        public DateTime? ModeratedDate { get; set; }
        public string? ReturnedReason { get; set; }
        public DateTime? ReturnedDate { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public virtual Assessment Assessment { get; set; }
        public virtual CourseOffering CourseOffering { get; set; }
        public virtual Unit Unit { get; set; }
    }
}
'@ | Set-Content -Path "$root\Entities\ModerationRecord.cs" -Encoding UTF8

Write-Host "Domain layer entities created successfully."
Write-Host "Files created: AssessmentType, Assessment, StudentAssessmentMark, AssessmentExemption,"
Write-Host "  AssessmentTemplate, GradingScale, GradeBand, CertificateRule,"
Write-Host "  StudentCertificateEligibility, GradeChangeHistory, UnitResult, ModerationRecord"
Write-Host "Enums created: CertificateEligibilityStatus, GradeChangeReason, MarkEntrySource, AssessmentVisibility"
