using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLaneAndHouseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_rooms_RoomId",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Lecturers_LecturerId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Semesters_SemesterId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Buildings_BuildingId",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Lecturers_LecturerId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Semesters_SemesterId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_units_UnitId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Departments_DepartmentId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureNotes_Lecturers_LecturerId",
                table: "LectureNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureNotes_units_UnitId",
                table: "LectureNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_AspNetUsers_UserId",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_LoginHistories_AspNetUsers_UserId",
                table: "LoginHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_Departments_DepartmentId",
                table: "Programmes");

            migrationBuilder.DropForeignKey(
                name: "FK_rooms_Blocks_BlockId1",
                table: "rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_rooms_Blocks_block_id",
                table: "rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Semesters_AcademicYears_AcademicYearId",
                table: "Semesters");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_units_UnitId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_Lecturers_LecturerId",
                table: "Timetables");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_Lecturers_LecturerId1",
                table: "Timetables");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_rooms_RoomId",
                table: "Timetables");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_units_UnitId",
                table: "Timetables");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Lecturers_LecturerId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Semesters_SemesterId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations");

            migrationBuilder.DropTable(
                name: "CourseProgramme");

            migrationBuilder.DropIndex(
                name: "IX_units_code",
                table: "units");

            migrationBuilder.DropIndex(
                name: "IX_UnitAllocations_LecturerId_UnitId_SemesterId",
                table: "UnitAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Timetables_LecturerId1",
                table: "Timetables");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_StudentId_UnitId_SemesterId",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Semesters_Name",
                table: "Semesters");

            migrationBuilder.DropIndex(
                name: "IX_rooms_BlockId1",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_Resource_PermissionType",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_ProgrammeUnits_ProgrammeId_UnitId",
                table: "ProgrammeUnits");

            migrationBuilder.DropIndex(
                name: "IX_Programmes_Code",
                table: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_Lecturers_DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropIndex(
                name: "IX_Lecturers_Email",
                table: "Lecturers");

            migrationBuilder.DropIndex(
                name: "IX_Lecturers_EmployeeNumber",
                table: "Lecturers");

            migrationBuilder.DropIndex(
                name: "IX_Grades_StudentId_UnitId_SemesterId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId_CourseId_SemesterId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Code",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_RoomNumber",
                table: "Classrooms");

            migrationBuilder.DropIndex(
                name: "IX_Classes_Code",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId_ClassId_Date",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_AssignmentId_StudentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AcademicYears_Name",
                table: "AcademicYears");

            migrationBuilder.DropColumn(
                name: "LecturerId1",
                table: "Timetables");

            migrationBuilder.DropColumn(
                name: "BlockId1",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Lecturers");

            migrationBuilder.AlterColumn<string>(
                name: "RoomNumber",
                table: "Timetables",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeek",
                table: "Timetables",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Semesters",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Resource",
                table: "RolePermissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "PermissionType",
                table: "RolePermissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Programmes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Programmes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Programmes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "LoginHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "LoginHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FailureReason",
                table: "LoginHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Lecturers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Lecturers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Lecturers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "Lecturers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Lecturers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "Grades",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "LetterGrade",
                table: "Grades",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Enrollments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Departments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Departments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Departments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Courses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Courses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Courses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CalendarEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "CalendarEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "CalendarEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CalendarEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Blocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Blocks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Blocks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "AuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserRole",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Attendances",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "Attendances",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AssignmentSubmissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "AssignmentSubmissions",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GraderName",
                table: "AssignmentSubmissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "AssignmentSubmissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "AssignmentSubmissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "AssignmentSubmissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "AssignmentSubmissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "Assignments",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Assignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Assignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "Assignments",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "LatePenaltyPercent",
                table: "Assignments",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Instructions",
                table: "Assignments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Assignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Attachments",
                table: "Assignments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleId1",
                table: "AspNetUserRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "AspNetUserRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoomId",
                table: "Accommodations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "HouseId",
                table: "Accommodations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LaneId",
                table: "Accommodations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "RoomId",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "HouseId",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LaneId",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId1",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AcademicYears",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "CourseProgrammes",
                columns: table => new
                {
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseProgrammes", x => new { x.CourseId, x.ProgrammeId });
                    table.ForeignKey(
                        name: "FK_CourseProgrammes_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseProgrammes_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaneName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NumberingFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartingHouseNumber = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Lanes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Houses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaneId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HouseNumberNumeric = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: false),
                    OccupantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccupiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VacatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Houses", x => x.id);
                    table.ForeignKey(
                        name: "FK_Houses_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Houses_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Houses_Students_OccupantId",
                        column: x => x.OccupantId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitAllocations_LecturerId",
                table: "UnitAllocations",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentId",
                table: "StudentEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeUnits_ProgrammeId",
                table: "ProgrammeUnits",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_StudentId",
                table: "Grades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId",
                table: "Attendances",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId",
                table: "AssignmentSubmissions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId1",
                table: "AspNetUserRoles",
                column: "RoleId1");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UserId1",
                table: "AspNetUserRoles",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_HouseId",
                table: "Accommodations",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_LaneId",
                table: "Accommodations",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_HouseId",
                table: "AccommodationAssignments",
                column: "HouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LaneId",
                table: "AccommodationAssignments",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseProgrammes_ProgrammeId",
                table: "CourseProgrammes",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_Houses_LaneId_HouseNumber",
                table: "Houses",
                columns: new[] { "LaneId", "HouseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Houses_OccupantId",
                table: "Houses",
                column: "OccupantId");

            migrationBuilder.CreateIndex(
                name: "IX_Houses_SemesterId",
                table: "Houses",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_LaneName_tenant_id",
                table: "Lanes",
                columns: new[] { "LaneName", "tenant_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Houses_HouseId",
                table: "AccommodationAssignments",
                column: "HouseId",
                principalTable: "Houses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Lanes_LaneId",
                table: "AccommodationAssignments",
                column: "LaneId",
                principalTable: "Lanes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Students_StudentId1",
                table: "AccommodationAssignments",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_rooms_RoomId",
                table: "AccommodationAssignments",
                column: "RoomId",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_Houses_HouseId",
                table: "Accommodations",
                column: "HouseId",
                principalTable: "Houses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_Lanes_LaneId",
                table: "Accommodations",
                column: "LaneId",
                principalTable: "Lanes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId1",
                table: "AspNetUserRoles",
                column: "RoleId1",
                principalTable: "AspNetRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId1",
                table: "AspNetUserRoles",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Lecturers_LecturerId",
                table: "Assignments",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Semesters_SemesterId",
                table: "Assignments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Buildings_BuildingId",
                table: "Blocks",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Lecturers_LecturerId",
                table: "Classes",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Semesters_SemesterId",
                table: "Classes",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_units_UnitId",
                table: "Classes",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Departments_DepartmentId",
                table: "Courses",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                table: "Courses",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_LectureNotes_Lecturers_LecturerId",
                table: "LectureNotes",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LectureNotes_units_UnitId",
                table: "LectureNotes",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturers_AspNetUsers_UserId",
                table: "Lecturers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId",
                table: "Lecturers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_LoginHistories_AspNetUsers_UserId",
                table: "LoginHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_Departments_DepartmentId",
                table: "Programmes",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_Blocks_block_id",
                table: "rooms",
                column: "block_id",
                principalTable: "Blocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Semesters_AcademicYears_AcademicYearId",
                table: "Semesters",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_units_UnitId",
                table: "StudentEnrollments",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_Lecturers_LecturerId",
                table: "Timetables",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_rooms_RoomId",
                table: "Timetables",
                column: "RoomId",
                principalTable: "rooms",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_units_UnitId",
                table: "Timetables",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_Lecturers_LecturerId",
                table: "UnitAllocations",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_Semesters_SemesterId",
                table: "UnitAllocations",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Houses_HouseId",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Lanes_LaneId",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Students_StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_rooms_RoomId",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_Houses_HouseId",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_Lanes_LaneId",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Lecturers_LecturerId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Semesters_SemesterId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Buildings_BuildingId",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Lecturers_LecturerId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Semesters_SemesterId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_units_UnitId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Departments_DepartmentId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureNotes_Lecturers_LecturerId",
                table: "LectureNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureNotes_units_UnitId",
                table: "LectureNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_AspNetUsers_UserId",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_LoginHistories_AspNetUsers_UserId",
                table: "LoginHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_Departments_DepartmentId",
                table: "Programmes");

            migrationBuilder.DropForeignKey(
                name: "FK_rooms_Blocks_block_id",
                table: "rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Semesters_AcademicYears_AcademicYearId",
                table: "Semesters");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_units_UnitId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_Lecturers_LecturerId",
                table: "Timetables");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_rooms_RoomId",
                table: "Timetables");

            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_units_UnitId",
                table: "Timetables");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Lecturers_LecturerId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Semesters_SemesterId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations");

            migrationBuilder.DropTable(
                name: "CourseProgrammes");

            migrationBuilder.DropTable(
                name: "Houses");

            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropIndex(
                name: "IX_UnitAllocations_LecturerId",
                table: "UnitAllocations");

            migrationBuilder.DropIndex(
                name: "IX_StudentEnrollments_StudentId",
                table: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_ProgrammeUnits_ProgrammeId",
                table: "ProgrammeUnits");

            migrationBuilder.DropIndex(
                name: "IX_Grades_StudentId",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmissions_AssignmentId",
                table: "AssignmentSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_UserId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_HouseId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_LaneId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_HouseId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LaneId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Success",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserRole",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "HouseId",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "LaneId",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "HouseId",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "LaneId",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "RoomNumber",
                table: "Timetables",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeek",
                table: "Timetables",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "LecturerId1",
                table: "Timetables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Semesters",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "BlockId1",
                table: "rooms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Resource",
                table: "RolePermissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionType",
                table: "RolePermissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Programmes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Programmes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Programmes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "LoginHistories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "LoginHistories",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FailureReason",
                table: "LoginHistories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Lecturers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Lecturers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Lecturers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "Lecturers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Lecturers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "Lecturers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "Grades",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "LetterGrade",
                table: "Grades",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Enrollments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Departments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Departments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Departments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Courses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Courses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Courses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CalendarEvents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "CalendarEvents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "CalendarEvents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CalendarEvents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Blocks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Blocks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Blocks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "AuditLogs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Attendances",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "Attendances",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AssignmentSubmissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "AssignmentSubmissions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GraderName",
                table: "AssignmentSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "AssignmentSubmissions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "AssignmentSubmissions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "AssignmentSubmissions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "AssignmentSubmissions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "Assignments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Assignments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Assignments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "Assignments",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "LatePenaltyPercent",
                table: "Assignments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Instructions",
                table: "Assignments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Assignments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Attachments",
                table: "Assignments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoomId",
                table: "Accommodations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoomId",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AcademicYears",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "CourseProgramme",
                columns: table => new
                {
                    CoursesId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseProgramme", x => new { x.CoursesId, x.ProgrammesId });
                    table.ForeignKey(
                        name: "FK_CourseProgramme_Courses_CoursesId",
                        column: x => x.CoursesId,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseProgramme_Programmes_ProgrammesId",
                        column: x => x.ProgrammesId,
                        principalTable: "Programmes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_units_code",
                table: "units",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitAllocations_LecturerId_UnitId_SemesterId",
                table: "UnitAllocations",
                columns: new[] { "LecturerId", "UnitId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_LecturerId1",
                table: "Timetables",
                column: "LecturerId1");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentId_UnitId_SemesterId",
                table: "StudentEnrollments",
                columns: new[] { "StudentId", "UnitId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_Name",
                table: "Semesters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_BlockId1",
                table: "rooms",
                column: "BlockId1");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_Resource_PermissionType",
                table: "RolePermissions",
                columns: new[] { "RoleId", "Resource", "PermissionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeUnits_ProgrammeId_UnitId",
                table: "ProgrammeUnits",
                columns: new[] { "ProgrammeId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_Code",
                table: "Programmes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_DepartmentId1",
                table: "Lecturers",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_Email",
                table: "Lecturers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_EmployeeNumber",
                table: "Lecturers",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_StudentId_UnitId_SemesterId",
                table: "Grades",
                columns: new[] { "StudentId", "UnitId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId_SemesterId",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_RoomNumber",
                table: "Classrooms",
                column: "RoomNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Code",
                table: "Classes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_ClassId_Date",
                table: "Attendances",
                columns: new[] { "StudentId", "ClassId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId_StudentId",
                table: "AssignmentSubmissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_Name",
                table: "AcademicYears",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseProgramme_ProgrammesId",
                table: "CourseProgramme",
                column: "ProgrammesId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_rooms_RoomId",
                table: "AccommodationAssignments",
                column: "RoomId",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Lecturers_LecturerId",
                table: "Assignments",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Semesters_SemesterId",
                table: "Assignments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Buildings_BuildingId",
                table: "Blocks",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Lecturers_LecturerId",
                table: "Classes",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Semesters_SemesterId",
                table: "Classes",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_units_UnitId",
                table: "Classes",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Departments_DepartmentId",
                table: "Courses",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                table: "Courses",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LectureNotes_Lecturers_LecturerId",
                table: "LectureNotes",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LectureNotes_units_UnitId",
                table: "LectureNotes",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturers_AspNetUsers_UserId",
                table: "Lecturers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId",
                table: "Lecturers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId1",
                table: "Lecturers",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_LoginHistories_AspNetUsers_UserId",
                table: "LoginHistories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_Departments_DepartmentId",
                table: "Programmes",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_Blocks_BlockId1",
                table: "rooms",
                column: "BlockId1",
                principalTable: "Blocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_Blocks_block_id",
                table: "rooms",
                column: "block_id",
                principalTable: "Blocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Semesters_AcademicYears_AcademicYearId",
                table: "Semesters",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_units_UnitId",
                table: "StudentEnrollments",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_Lecturers_LecturerId",
                table: "Timetables",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_Lecturers_LecturerId1",
                table: "Timetables",
                column: "LecturerId1",
                principalTable: "Lecturers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_rooms_RoomId",
                table: "Timetables",
                column: "RoomId",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_units_UnitId",
                table: "Timetables",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_Lecturers_LecturerId",
                table: "UnitAllocations",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_Semesters_SemesterId",
                table: "UnitAllocations",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
