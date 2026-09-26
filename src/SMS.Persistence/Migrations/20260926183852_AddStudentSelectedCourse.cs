using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentSelectedCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SelectedCourseId",
                table: "Students",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_SelectedCourseId",
                table: "Students",
                column: "SelectedCourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Courses_SelectedCourseId",
                table: "Students",
                column: "SelectedCourseId",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // ─────────────────────────────────────────────────────────────────
            // Backfill (additive, non-destructive, idempotent).
            //
            // Students who registered BEFORE this fix never had their course
            // choice persisted anywhere, so the value must be reconstructed from
            // data that already exists. Each step below only writes a value when
            // the candidate course is UNAMBIGUOUS for that student, and every
            // step is restricted to the student's own tenant. Anything that stays
            // NULL is intentional: those students are simply asked to select a
            // course again, which is the behaviour they already had.
            //
            // This backfill only ever populates a new nullable column. It never
            // drops, truncates or rewrites existing registration data.
            // ─────────────────────────────────────────────────────────────────

            // 1. Students who already completed the course-selection wizard:
            //    their unit-level Enrollments rows record the chosen course.
            //    Only applied when the student has exactly one distinct course,
            //    so a student enrolled across two courses is left for staff.
            migrationBuilder.Sql(@"
UPDATE ""Students"" s
SET ""SelectedCourseId"" = src.""CourseId""
FROM (
    SELECT ""StudentId"", (array_agg(""CourseId""))[1] AS ""CourseId""
    FROM ""Enrollments""
    WHERE ""is_deleted"" = false
    GROUP BY ""StudentId""
    HAVING COUNT(DISTINCT ""CourseId"") = 1
) src
WHERE s.""id"" = src.""StudentId""
  AND s.""SelectedCourseId"" IS NULL
  AND s.""is_deleted"" = false
  AND EXISTS (
      SELECT 1 FROM ""Courses"" c
      WHERE c.""id"" = src.""CourseId""
        AND c.""tenant_id"" = s.""tenant_id""
  );");

            // 2. Students with an approved, offering-backed enrollment and no
            //    wizard Enrollments: recover the course from the offering. Again
            //    only when it is unambiguous.
            migrationBuilder.Sql(@"
UPDATE ""Students"" s
SET ""SelectedCourseId"" = src.""CourseId""
FROM (
    SELECT coe.""StudentId"", (array_agg(o.""CourseId""))[1] AS ""CourseId""
    FROM ""course_offering_enrollments"" coe
    INNER JOIN ""course_offerings"" o ON o.""id"" = coe.""CourseOfferingId""
    WHERE coe.""is_deleted"" = false
    GROUP BY coe.""StudentId""
    HAVING COUNT(DISTINCT o.""CourseId"") = 1
) src
WHERE s.""id"" = src.""StudentId""
  AND s.""SelectedCourseId"" IS NULL
  AND s.""is_deleted"" = false
  AND EXISTS (
      SELECT 1 FROM ""Courses"" c
      WHERE c.""id"" = src.""CourseId""
        AND c.""tenant_id"" = s.""tenant_id""
  );");

            // 3. Last resort for the students this bug actually affected: the old
            //    registration wrote only the programme, so recover the course
            //    when that programme has exactly ONE active course. Ambiguous
            //    programmes are deliberately left NULL so the student re-selects
            //    rather than being silently given the wrong course.
            migrationBuilder.Sql(@"
UPDATE ""Students"" s
SET ""SelectedCourseId"" = src.""CourseId""
FROM (
    SELECT ""ProgrammeId"", (array_agg(""id""))[1] AS ""CourseId""
    FROM ""Courses""
    WHERE ""is_deleted"" = false
      AND ""IsActive"" = true
      AND ""ProgrammeId"" IS NOT NULL
    GROUP BY ""ProgrammeId""
    HAVING COUNT(*) = 1
) src
WHERE s.""ProgrammeId"" = src.""ProgrammeId""
  AND s.""SelectedCourseId"" IS NULL
  AND s.""is_deleted"" = false
  AND s.""ProgrammeId"" IS NOT NULL
  -- Explicit tenant guard: the recovered course must belong to the same
  -- tenant as the student, even though a programme id is already tenant-unique.
  AND EXISTS (
      SELECT 1 FROM ""Courses"" c
      WHERE c.""id"" = src.""CourseId""
        AND c.""tenant_id"" = s.""tenant_id""
  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Courses_SelectedCourseId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_SelectedCourseId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SelectedCourseId",
                table: "Students");
        }
    }
}
