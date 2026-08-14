using Microsoft.EntityFrameworkCore;
using SMS.Domain.Entities;
using SMS.Domain.Interfaces;

namespace SMS.Persistence.Data
{
    public partial class ApplicationDbContext
    {
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<AssessmentType> AssessmentTypes { get; set; }
        public DbSet<AssessmentTemplate> AssessmentTemplates { get; set; }
        public DbSet<StudentAssessmentMark> StudentAssessmentMarks { get; set; }
        public DbSet<AssessmentExemption> AssessmentExemptions { get; set; }
        public DbSet<GradingScale> GradingScales { get; set; }
        public DbSet<GradeBand> GradeBands { get; set; }
        public DbSet<CertificateRule> CertificateRules { get; set; }
        public DbSet<StudentCertificateEligibility> StudentCertificateEligibilities { get; set; }
        public DbSet<GradeChangeHistory> GradeChangeHistories { get; set; }
        public DbSet<UnitResult> UnitResults { get; set; }
        public DbSet<ModerationRecord> ModerationRecords { get; set; }
    }
}
