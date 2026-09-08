using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <summary>
    /// Seeds the configurable assessment / grading data required by the
    /// centralized Assessment Engine and adds moderation record metadata.
    /// Idempotent: rows are only inserted when absent per tenant. No existing
    /// rows are modified or deleted. Inserts are aligned to the resolved
    /// tenant so PostgreSQL Row Level Security is honoured.
    /// </summary>
    public partial class SeedAssessmentGradingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid?>(
                name: "StudentId",
                table: "ModerationRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid?>(
                name: "MarkId",
                table: "ModerationRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal?>(
                name: "OriginalScore",
                table: "ModerationRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal?>(
                name: "RevisedScore",
                table: "ModerationRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string?>(
                name: "ReviewerComments",
                table: "ModerationRecords",
                type: "text",
                nullable: true);
            // 13 institutional assessment types (configurable; admins may add more).
            migrationBuilder.Sql(
                "SELECT set_config('app.tenant_id', COALESCE((SELECT \"Id\"::text FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), ''), false);\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Assignment', 'ASSIGNMENT', 'Written assignments', 1, true, true, 1, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'ASSIGNMENT' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Practical', 'PRACTICAL', 'Practical exercises', 2, true, true, 2, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'PRACTICAL' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Laboratory', 'LABORATORY', 'Lab work', 3, true, true, 3, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'LABORATORY' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'CAT', 'CAT', 'Continuous Assessment Test', 4, true, true, 4, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'CAT' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Quiz', 'QUIZ', 'Online quizzes', 5, true, true, 5, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'QUIZ' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Oral Examination', 'ORAL', 'Oral presentations and exams', 6, true, true, 6, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'ORAL' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Project', 'PROJECT', 'Major project work', 7, true, true, 7, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'PROJECT' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Presentation', 'PRESENTATION', 'Class presentations', 8, true, true, 8, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'PRESENTATION' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Final Examination', 'FINALEXAM', 'End-of-term final exam', 9, true, true, 9, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'FINALEXAM' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Supplementary Examination', 'SUPP', 'Supplementary retake exam', 10, true, true, 10, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'SUPP' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Retake Examination', 'RETAKE', 'Full retake examination', 11, true, true, 11, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'RETAKE' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Coursework', 'COURSEWORK', 'Overall coursework component', 12, true, true, 12, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'COURSEWORK' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"AssessmentTypes\" (\"Id\", \"Name\", \"Code\", \"Description\", \"Category\", \"IsActive\", \"IsSystemDefined\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT gen_random_uuid(), 'Participation', 'PARTICIPATION', 'Class participation and attendance', 13, true, true, 13, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"AssessmentTypes\" x WHERE x.\"Code\" = 'PARTICIPATION' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n");

            // Default grading scale with 4 bands.
            migrationBuilder.Sql(
                "SELECT set_config('app.tenant_id', COALESCE((SELECT \"Id\"::text FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), ''), false);\n" +
                "INSERT INTO \"GradingScales\" (\"Id\", \"Name\", \"Description\", \"Version\", \"IsActive\", \"IsDefault\", \"EffectiveFrom\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT '00000000-0000-0000-0000-000000000001', 'Default Grading Scale', 'Standard 4-band grading scale', 1, true, true, now(), COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"GradingScales\" x WHERE x.\"Id\" = '00000000-0000-0000-0000-000000000001' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"GradeBands\" (\"Id\", \"GradingScaleId\", \"GradeLetter\", \"Description\", \"MinPercentage\", \"MaxPercentage\", \"GpaPoints\", \"ColorCode\", \"HonorsClassification\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'A', 'Distinction', 75.00, 100.00, 4.0, '#00AA00', NULL, 1, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"GradeBands\" x WHERE x.\"Id\" = '00000000-0000-0000-0000-000000000001' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"GradeBands\" (\"Id\", \"GradingScaleId\", \"GradeLetter\", \"Description\", \"MinPercentage\", \"MaxPercentage\", \"GpaPoints\", \"ColorCode\", \"HonorsClassification\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT '00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'B', 'Credit', 65.00, 74.99, 3.0, '#0000FF', NULL, 2, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"GradeBands\" x WHERE x.\"Id\" = '00000000-0000-0000-0000-000000000002' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"GradeBands\" (\"Id\", \"GradingScaleId\", \"GradeLetter\", \"Description\", \"MinPercentage\", \"MaxPercentage\", \"GpaPoints\", \"ColorCode\", \"HonorsClassification\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT '00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'C', 'Pass', 50.00, 64.99, 2.0, '#FFA500', NULL, 3, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"GradeBands\" x WHERE x.\"Id\" = '00000000-0000-0000-0000-000000000003' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n" +
                "INSERT INTO \"GradeBands\" (\"Id\", \"GradingScaleId\", \"GradeLetter\", \"Description\", \"MinPercentage\", \"MaxPercentage\", \"GpaPoints\", \"ColorCode\", \"HonorsClassification\", \"SortOrder\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT '00000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'F', 'Fail', 0.00, 49.99, 0.0, '#FF0000', NULL, 4, COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"GradeBands\" x WHERE x.\"Id\" = '00000000-0000-0000-0000-000000000004' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n");

            // Default certificate eligibility rule.
            migrationBuilder.Sql(
                "SELECT set_config('app.tenant_id', COALESCE((SELECT \"Id\"::text FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), ''), false);\n" +
                "INSERT INTO \"CertificateRules\" (\"Id\", \"Name\", \"Description\", \"MinimumPassingPercentage\", \"MinimumPassingGradeLetter\", \"RequireAllMandatoryAssessments\", \"RequireNoOutstandingIncomplete\", \"RequireAllRequiredUnits\", \"IsActive\", \"IsVersioned\", \"Version\", \"EffectiveFrom\", \"tenant_id\", \"created_at\", \"updated_at\", \"is_deleted\")\n" +
                "SELECT '00000000-0000-0000-0000-000000000001', 'Default Certificate Rule', 'Default certificate eligibility rule', 50.00, 'F', true, true, true, true, true, 1, now(), COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'), now(), now(), false\n" +
                "WHERE NOT EXISTS (SELECT 1 FROM \"CertificateRules\" x WHERE x.\"Id\" = '00000000-0000-0000-0000-000000000001' AND x.\"tenant_id\" = COALESCE((SELECT \"Id\" FROM \"Tenants\" WHERE \"IsActive\" = true ORDER BY \"CreatedDate\" LIMIT 1), '00000000-0000-0000-0000-000000000000'));\n");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReviewerComments", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "RevisedScore", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "OriginalScore", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "MarkId", table: "ModerationRecords");
            migrationBuilder.DropColumn(name: "StudentId", table: "ModerationRecords");
        }
    }
}
