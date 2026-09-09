using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Seeds a single default/current semester when the semester table is
    /// completely empty. Accommodation assignments require a SemesterId and the
    /// assignment workflow resolves the current semester server-side when the
    /// client does not supply one. This migration guarantees at least one
    /// semester exists so the receptionist accommodation workflow functions on
    /// a clean production database (idempotent: no-op when any semester exists).
    /// </summary>
    public partial class SeedDefaultSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"INSERT INTO ""Semesters"" (""id"", ""Name"", ""SemesterNumber"", ""StartDate"", ""EndDate"", ""AcademicYearId"", ""IsActive"", ""IsCurrent"", ""tenant_id"", ""created_at"", ""updated_at"", ""created_date"", ""is_deleted"")
                  SELECT gen_random_uuid(), 'Semester 1 (Default)', 1, now(), now() + interval '120 days', NULL, true, true,
                         COALESCE((SELECT ""id"" FROM ""Tenants"" WHERE ""IsActive"" = true ORDER BY ""created_date"" LIMIT 1), '00000000-0000-0000-0000-000000000000'),
                         now(), now(), now(), false
                  WHERE NOT EXISTS (
                      SELECT 1 FROM ""Semesters"" WHERE ""is_deleted"" = false
                  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration intentionally does not delete user-created data.
            // The default semester deliberately persists after a rollback.
        }
    }
}
