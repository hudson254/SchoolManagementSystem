using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    public partial class EnableRowLevelSecurity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTenantContextFunction(migrationBuilder);
            string[] t = {"AcademicYears","AccommodationAssignments","Accommodations",
                "AssessmentExemptions","AssessmentTemplates","AssessmentTypes",
                "Assessments","AssignmentIssueReports","Assignments",
                "AssignmentSubmissions","Attendances","AuditLogs","Blocks",
                "Buildings","CalendarEvents","CertificateRules","Classes",
                "Classrooms","Courses","CourseOfferings",
                "CourseOfferingEnrollments","CourseOfferingLecturers",
                "CourseOfferingUnits","Departments","Enrollments","GradeBands",
                "GradeChangeHistories","Grades","GradingScales","Houses","Lanes",
                "LectureNotes","Lecturers","LoginHistories","ModerationRecords",
                "Notifications","PasswordResetRequests","Programmes",
                "ProgrammeUnits","ReportVerifications","RolePermissions","Rooms",
                "Semesters","StudentAssessmentMarks",
                "StudentCertificateEligibilities","StudentEnrollments","Students",
                "Tenants","Timetables","Titles","UnitAllocations","UnitResults",
                "Units","UploadFiles"};
            foreach (var x in t) EnableRls(migrationBuilder, x, "tenant_id");
            EnableRls(migrationBuilder, "AspNetUsers", "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            string[] t = {"AcademicYears","AccommodationAssignments","Accommodations",
                "AssessmentExemptions","AssessmentTemplates","AssessmentTypes",
                "Assessments","AssignmentIssueReports","Assignments",
                "AssignmentSubmissions","Attendances","AuditLogs","Blocks",
                "Buildings","CalendarEvents","CertificateRules","Classes",
                "Classrooms","Courses","CourseOfferings",
                "CourseOfferingEnrollments","CourseOfferingLecturers",
                "CourseOfferingUnits","Departments","Enrollments","GradeBands",
                "GradeChangeHistories","Grades","GradingScales","Houses","Lanes",
                "LectureNotes","Lecturers","LoginHistories","ModerationRecords",
                "Notifications","PasswordResetRequests","Programmes",
                "ProgrammeUnits","ReportVerifications","RolePermissions","Rooms",
                "Semesters","StudentAssessmentMarks",
                "StudentCertificateEligibilities","StudentEnrollments","Students",
                "Tenants","Timetables","Titles","UnitAllocations","UnitResults",
                "Units","UploadFiles"};
            foreach (var x in t) DisableRls(migrationBuilder, x);
            DisableRls(migrationBuilder, "AspNetUsers");
        }

        private void CreateTenantContextFunction(MigrationBuilder mb)
        {
            mb.Sql(@"
                CREATE OR REPLACE FUNCTION app.current_tenant_id()
                RETURNS uuid LANGUAGE plpgsql STABLE AS $func$
                DECLARE tid text;
                BEGIN
                    tid := current_setting('app.tenant_id', true);
                    IF tid IS NULL OR tid = '' THEN
                        RETURN '00000000-0000-0000-0000-000000000000'::uuid;
                    END IF;
                    RETURN tid::uuid;
                EXCEPTION WHEN OTHERS THEN
                    RETURN '00000000-0000-0000-0000-000000000000'::uuid;
                END;
                $func$;
            ");
        }

        private void EnableRls(MigrationBuilder mb, string table, string col = "tenant_id")
        {
            mb.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
            mb.Sql($"ALTER TABLE \"{table}\" FORCE ROW LEVEL SECURITY;");
            mb.Sql($"DROP POLICY IF EXISTS tenant_sel_{table} ON \"{table}\";");
            mb.Sql($"DROP POLICY IF EXISTS tenant_ins_{table} ON \"{table}\";");
            mb.Sql($"DROP POLICY IF EXISTS tenant_upd_{table} ON \"{table}\";");
            mb.Sql($"DROP POLICY IF EXISTS tenant_del_{table} ON \"{table}\";");
            mb.Sql($"CREATE POLICY tenant_sel_{table} ON \"{table}\" FOR SELECT USING ({col} = app.current_tenant_id());");
            mb.Sql($"CREATE POLICY tenant_ins_{table} ON \"{table}\" FOR INSERT WITH CHECK ({col} = app.current_tenant_id());");
            mb.Sql($"CREATE POLICY tenant_upd_{table} ON \"{table}\" FOR UPDATE USING ({col} = app.current_tenant_id()) WITH CHECK ({col} = app.current_tenant_id());");
            mb.Sql($"CREATE POLICY tenant_del_{table} ON \"{table}\" FOR DELETE USING ({col} = app.current_tenant_id());");
        }

        private void DisableRls(MigrationBuilder mb, string table)
        {
            mb.Sql($"DROP POLICY IF EXISTS tenant_sel_{table} ON \"{table}\";");
            mb.Sql($"DROP POLICY IF EXISTS tenant_ins_{table} ON \"{table}\";");
            mb.Sql($"DROP POLICY IF EXISTS tenant_upd_{table} ON \"{table}\";");
            mb.Sql($"DROP POLICY IF EXISTS tenant_del_{table} ON \"{table}\";");
            mb.Sql($"ALTER TABLE \"{table}\" NO FORCE ROW LEVEL SECURITY;");
            mb.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
        }
    }
}