using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixProductionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Semesters_SemesterId",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId1",
                table: "AspNetUserRoles");

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
                name: "FK_course_offerings_AcademicYears_AcademicYearId",
                table: "course_offerings");

            migrationBuilder.DropForeignKey(
                name: "FK_course_offerings_Courses_CourseId",
                table: "course_offerings");

            migrationBuilder.DropForeignKey(
                name: "FK_course_offerings_Semesters_SemesterId",
                table: "course_offerings");

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
                name: "FK_Lecturers_Departments_DepartmentId",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Students_StudentId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_units_UnitId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Programmes_ProgrammeId1",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Lecturers_LecturerId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Semesters_SemesterId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_course_offerings_CourseOfferingId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations");

            migrationBuilder.DropIndex(
                name: "IX_Students_ProgrammeId1",
                table: "Students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProgrammeUnits",
                table: "ProgrammeUnits");

            migrationBuilder.DropIndex(
                name: "IX_ProgrammeUnits_ProgrammeId",
                table: "ProgrammeUnits");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_UserId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "ProgrammeId1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RoleId1",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "AspNetUserRoles");

            migrationBuilder.AddColumn<string>(
                name: "NationalIdPassport",
                table: "Students",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationStatus",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StaffIdEstNo",
                table: "Students",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "Lecturers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdPassport",
                table: "Lecturers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationStatus",
                table: "Lecturers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefreshTokenFamilyId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProgrammeUnits",
                table: "ProgrammeUnits",
                columns: new[] { "ProgrammeId", "UnitId" });

            migrationBuilder.CreateTable(
                name: "upload_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GeneratedFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Extension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UploadedByUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VirusScanResult = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VirusScanDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDuplicate = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LecturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_upload_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_upload_files_upload_files_OriginalFileId",
                        column: x => x.OriginalFileId,
                        principalTable: "upload_files",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_DepartmentId1",
                table: "Lecturers",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId1",
                table: "Courses",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_course_offerings_OfferingCode",
                table: "course_offerings",
                column: "OfferingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments",
                column: "LecturerId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments",
                column: "StudentId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_upload_files_OriginalFileId",
                table: "upload_files",
                column: "OriginalFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Semesters_SemesterId",
                table: "AccommodationAssignments",
                column: "SemesterId",
                principalTable: "Semesters",
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
                name: "FK_course_offerings_AcademicYears_AcademicYearId",
                table: "course_offerings",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_course_offerings_Courses_CourseId",
                table: "course_offerings",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_course_offerings_Semesters_SemesterId",
                table: "course_offerings",
                column: "SemesterId",
                principalTable: "Semesters",
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
                name: "FK_Courses_Departments_DepartmentId1",
                table: "Courses",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "id");

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
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_Students_StudentId",
                table: "StudentEnrollments",
                column: "StudentId",
                principalTable: "Students",
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
                name: "FK_UnitAllocations_course_offerings_CourseOfferingId",
                table: "UnitAllocations",
                column: "CourseOfferingId",
                principalTable: "course_offerings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Semesters_SemesterId",
                table: "AccommodationAssignments");

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
                name: "FK_course_offerings_AcademicYears_AcademicYearId",
                table: "course_offerings");

            migrationBuilder.DropForeignKey(
                name: "FK_course_offerings_Courses_CourseId",
                table: "course_offerings");

            migrationBuilder.DropForeignKey(
                name: "FK_course_offerings_Semesters_SemesterId",
                table: "course_offerings");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Departments_DepartmentId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Departments_DepartmentId1",
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
                name: "FK_Lecturers_Departments_DepartmentId",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_Students_StudentId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentEnrollments_units_UnitId",
                table: "StudentEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Lecturers_LecturerId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_Semesters_SemesterId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_course_offerings_CourseOfferingId",
                table: "UnitAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations");

            migrationBuilder.DropTable(
                name: "upload_files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProgrammeUnits",
                table: "ProgrammeUnits");

            migrationBuilder.DropIndex(
                name: "IX_Lecturers_DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropIndex(
                name: "IX_Courses_DepartmentId1",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_course_offerings_OfferingCode",
                table: "course_offerings");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "NationalIdPassport",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RegistrationStatus",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StaffIdEstNo",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "NationalIdPassport",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "RegistrationStatus",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RefreshTokenFamilyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "ProgrammeId1",
                table: "Students",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProgrammeUnits",
                table: "ProgrammeUnits",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ProgrammeId1",
                table: "Students",
                column: "ProgrammeId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeUnits_ProgrammeId",
                table: "ProgrammeUnits",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId1",
                table: "AspNetUserRoles",
                column: "RoleId1");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UserId1",
                table: "AspNetUserRoles",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments",
                column: "LecturerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments",
                column: "LecturerId1");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments",
                column: "StudentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Semesters_SemesterId",
                table: "AccommodationAssignments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_course_offerings_AcademicYears_AcademicYearId",
                table: "course_offerings",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_course_offerings_Courses_CourseId",
                table: "course_offerings",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_course_offerings_Semesters_SemesterId",
                table: "course_offerings",
                column: "SemesterId",
                principalTable: "Semesters",
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
                name: "FK_Lecturers_Departments_DepartmentId",
                table: "Lecturers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_Semesters_SemesterId",
                table: "StudentEnrollments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentEnrollments_Students_StudentId",
                table: "StudentEnrollments",
                column: "StudentId",
                principalTable: "Students",
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
                name: "FK_Students_Programmes_ProgrammeId1",
                table: "Students",
                column: "ProgrammeId1",
                principalTable: "Programmes",
                principalColumn: "id");

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
                name: "FK_UnitAllocations_course_offerings_CourseOfferingId",
                table: "UnitAllocations",
                column: "CourseOfferingId",
                principalTable: "course_offerings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitAllocations_units_UnitId",
                table: "UnitAllocations",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
