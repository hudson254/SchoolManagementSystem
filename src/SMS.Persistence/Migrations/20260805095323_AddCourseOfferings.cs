using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseOfferings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseOfferingId",
                table: "UnitAllocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseOfferingId",
                table: "Grades",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseOfferingId",
                table: "Enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseOfferingId",
                table: "Attendances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseOfferingId",
                table: "Assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "course_offerings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferingCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intake = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegistrationStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegistrationEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_offerings", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_offerings_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_offerings_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_offerings_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    RequestedEmail = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FulfilledByUserId = table.Column<string>(type: "text", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportVerifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReportType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReportName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GeneratedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GeneratedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SHA256Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HashAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationCount = table.Column<int>(type: "integer", nullable: false),
                    LastVerified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WatermarkEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WatermarkText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportVerifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_ReportVerifications_AspNetUsers_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "course_offering_enrollments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationStatus = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DropDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_offering_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_offering_enrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_offering_enrollments_course_offerings_CourseOffering~",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course_offering_lecturers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LecturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmationStatus = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_offering_lecturers", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_offering_lecturers_Lecturers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Lecturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_offering_lecturers_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course_offering_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Credits = table.Column<int>(type: "integer", nullable: false),
                    ContactHours = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    LearningOutcomes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssessmentMethods = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssessmentWeighting = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_offering_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_offering_units_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_offering_units_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "assignment_issue_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CourseOfferingEnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseOfferingLecturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_issue_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignment_issue_reports_course_offering_enrollments_Course~",
                        column: x => x.CourseOfferingEnrollmentId,
                        principalTable: "course_offering_enrollments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_assignment_issue_reports_course_offering_lecturers_CourseOf~",
                        column: x => x.CourseOfferingLecturerId,
                        principalTable: "course_offering_lecturers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_assignment_issue_reports_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitAllocations_CourseOfferingId",
                table: "UnitAllocations",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_created_date",
                table: "Notifications",
                column: "created_date");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_IsSuccessful_LoginTime",
                table: "LoginHistories",
                columns: new[] { "IsSuccessful", "LoginTime" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_LoginTime",
                table: "LoginHistories",
                column: "LoginTime");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_UserId_LoginTime",
                table: "LoginHistories",
                columns: new[] { "UserId", "LoginTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Grades_CourseOfferingId",
                table: "Grades",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_StudentId_UnitId",
                table: "Grades",
                columns: new[] { "StudentId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseOfferingId",
                table: "Enrollments",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EnrollmentDate",
                table: "Enrollments",
                column: "EnrollmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Status",
                table: "Enrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName",
                table: "AuditLogs",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CourseOfferingId",
                table: "Attendances",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CourseOfferingId",
                table: "Assignments",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_issue_reports_CourseOfferingEnrollmentId",
                table: "assignment_issue_reports",
                column: "CourseOfferingEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_issue_reports_CourseOfferingId",
                table: "assignment_issue_reports",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_issue_reports_CourseOfferingLecturerId",
                table: "assignment_issue_reports",
                column: "CourseOfferingLecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offering_enrollments_CourseOfferingId",
                table: "course_offering_enrollments",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offering_enrollments_StudentId",
                table: "course_offering_enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offering_lecturers_CourseOfferingId",
                table: "course_offering_lecturers",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offering_lecturers_LecturerId",
                table: "course_offering_lecturers",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offering_units_CourseOfferingId",
                table: "course_offering_units",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offering_units_UnitId",
                table: "course_offering_units",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offerings_AcademicYearId",
                table: "course_offerings",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offerings_CourseId",
                table: "course_offerings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_course_offerings_SemesterId",
                table: "course_offerings",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportVerifications_GeneratedByUserId",
                table: "ReportVerifications",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportVerifications_GeneratedDate",
                table: "ReportVerifications",
                column: "GeneratedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReportVerifications_ReportId",
                table: "ReportVerifications",
                column: "ReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportVerifications_ReportType",
                table: "ReportVerifications",
                column: "ReportType");

            migrationBuilder.CreateIndex(
                name: "IX_ReportVerifications_Status",
                table: "ReportVerifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReportVerifications_VerificationToken",
                table: "ReportVerifications",
                column: "VerificationToken",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_course_offerings_CourseOfferingId",
                table: "Assignments",
                column: "CourseOfferingId",
                principalTable: "course_offerings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_course_offerings_CourseOfferingId",
                table: "Attendances",
                column: "CourseOfferingId",
                principalTable: "course_offerings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_course_offerings_CourseOfferingId",
                table: "Enrollments",
                column: "CourseOfferingId",
                principalTable: "course_offerings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_course_offerings_CourseOfferingId",
                table: "Grades",
                column: "CourseOfferingId",
                principalTable: "course_offerings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_course_offerings_CourseOfferingId",
                table: "UnitAllocations",
                column: "CourseOfferingId",
                principalTable: "course_offerings",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_course_offerings_CourseOfferingId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_course_offerings_CourseOfferingId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_course_offerings_CourseOfferingId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_course_offerings_CourseOfferingId",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_course_offerings_CourseOfferingId",
                table: "UnitAllocations");

            migrationBuilder.DropTable(
                name: "assignment_issue_reports");

            migrationBuilder.DropTable(
                name: "course_offering_units");

            migrationBuilder.DropTable(
                name: "PasswordResetRequests");

            migrationBuilder.DropTable(
                name: "ReportVerifications");

            migrationBuilder.DropTable(
                name: "course_offering_enrollments");

            migrationBuilder.DropTable(
                name: "course_offering_lecturers");

            migrationBuilder.DropTable(
                name: "course_offerings");

            migrationBuilder.DropIndex(
                name: "IX_UnitAllocations_CourseOfferingId",
                table: "UnitAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_created_date",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_LoginHistories_IsSuccessful_LoginTime",
                table: "LoginHistories");

            migrationBuilder.DropIndex(
                name: "IX_LoginHistories_LoginTime",
                table: "LoginHistories");

            migrationBuilder.DropIndex(
                name: "IX_LoginHistories_UserId_LoginTime",
                table: "LoginHistories");

            migrationBuilder.DropIndex(
                name: "IX_Grades_CourseOfferingId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_StudentId_UnitId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_CourseOfferingId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_EnrollmentDate",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_Status",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CourseOfferingId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_CourseOfferingId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CourseOfferingId",
                table: "UnitAllocations");

            migrationBuilder.DropColumn(
                name: "CourseOfferingId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "CourseOfferingId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "CourseOfferingId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CourseOfferingId",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
