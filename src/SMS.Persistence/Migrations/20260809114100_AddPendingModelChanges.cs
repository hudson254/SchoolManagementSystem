using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Students",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Lecturers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Lecturers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentTypes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemDefined = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_AssessmentTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    MinimumPassingPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    MinimumPassingGradeLetter = table.Column<string>(type: "text", nullable: true),
                    RequireAllMandatoryAssessments = table.Column<bool>(type: "boolean", nullable: false),
                    RequireNoOutstandingIncomplete = table.Column<bool>(type: "boolean", nullable: false),
                    RequireAllRequiredUnits = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalRequirements = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVersioned = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LogoPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WatermarkPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FieldMappings = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalSignatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradingScales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingScales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Titles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_Titles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTemplates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric", nullable: false),
                    AssessmentTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresModeration = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_AssessmentTemplates", x => x.id);
                    table.ForeignKey(
                        name: "FK_AssessmentTemplates_AssessmentTypes_AssessmentTypeId",
                        column: x => x.AssessmentTypeId,
                        principalTable: "AssessmentTypes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "StudentCertificateEligibilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OverallPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    OverallGradeLetter = table.Column<string>(type: "text", nullable: true),
                    HasOutstandingIncomplete = table.Column<bool>(type: "boolean", nullable: false),
                    HasFailedRequiredUnits = table.Column<bool>(type: "boolean", nullable: false),
                    EligibilityDetails = table.Column<string>(type: "text", nullable: true),
                    EvaluatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluatedBy = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_StudentCertificateEligibilities", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentCertificateEligibilities_CertificateRules_Certificat~",
                        column: x => x.CertificateRuleId,
                        principalTable: "CertificateRules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StudentCertificateEligibilities_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersion = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FinalGrade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Classification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VerificationUrl = table.Column<string>(type: "text", nullable: false),
                    QrCodePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ParentCertificateId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupersedesCertificateId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_CertificateTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CertificateTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Certificates_ParentCertificateId",
                        column: x => x.ParentCertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GradeBands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingScaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    GradeLetter = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    GpaPoints = table.Column<decimal>(type: "numeric", nullable: true),
                    ColorCode = table.Column<string>(type: "text", nullable: false),
                    HonorsClassification = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_GradeBands", x => x.id);
                    table.ForeignKey(
                        name: "FK_GradeBands_GradingScales_GradingScaleId",
                        column: x => x.GradingScaleId,
                        principalTable: "GradingScales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnitResults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: true),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradingScaleVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    GradeLetter = table.Column<string>(type: "text", nullable: false),
                    GradeDescription = table.Column<string>(type: "text", nullable: false),
                    GpaPoints = table.Column<decimal>(type: "numeric", nullable: true),
                    PublicationStatus = table.Column<int>(type: "integer", nullable: false),
                    ModerationStatus = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedBy = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    IsRecalculated = table.Column<bool>(type: "boolean", nullable: false),
                    LastCalculatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCalculatedBy = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_UnitResults", x => x.id);
                    table.ForeignKey(
                        name: "FK_UnitResults_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_UnitResults_GradingScales_GradingScaleVersionId",
                        column: x => x.GradingScaleVersionId,
                        principalTable: "GradingScales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_UnitResults_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_UnitResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnitResults_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_UnitResults_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: true),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessmentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LecturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessmentTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AllowLateSubmission = table.Column<bool>(type: "boolean", nullable: false),
                    LatePenaltyPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: true),
                    IsOnlineSubmission = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsExemptable = table.Column<bool>(type: "boolean", nullable: false),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresModeration = table.Column<bool>(type: "boolean", nullable: false),
                    IsAnonymousMarking = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublicationStatus = table.Column<int>(type: "integer", nullable: false),
                    ModerationStatus = table.Column<int>(type: "integer", nullable: false),
                    IsWeightLocked = table.Column<bool>(type: "boolean", nullable: false),
                    WeightLockedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WeightLockedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    FeedbackTemplate = table.Column<string>(type: "text", nullable: true),
                    CreatedByLecturerId = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_Assessments_AssessmentTemplates_AssessmentTemplateId",
                        column: x => x.AssessmentTemplateId,
                        principalTable: "AssessmentTemplates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Assessments_AssessmentTypes_AssessmentTypeId",
                        column: x => x.AssessmentTypeId,
                        principalTable: "AssessmentTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assessments_Assignments_LinkedAssignmentId",
                        column: x => x.LinkedAssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Assessments_Lecturers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Lecturers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Assessments_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Assessments_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Assessments_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificateAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateAuditLogs_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentExemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    GrantedBy = table.Column<string>(type: "text", nullable: true),
                    GrantedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_AssessmentExemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_AssessmentExemptions_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentExemptions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModerationRecords",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    ModeratedBy = table.Column<string>(type: "text", nullable: true),
                    ModeratedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnedReason = table.Column<string>(type: "text", nullable: true),
                    ReturnedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ModerationRecords", x => x.id);
                    table.ForeignKey(
                        name: "FK_ModerationRecords_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModerationRecords_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ModerationRecords_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "StudentAssessmentMarks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mark = table.Column<decimal>(type: "numeric", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    WeightedScore = table.Column<decimal>(type: "numeric", nullable: false),
                    IsDraft = table.Column<bool>(type: "boolean", nullable: false),
                    IsModerated = table.Column<bool>(type: "boolean", nullable: false),
                    ModeratedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModeratedBy = table.Column<string>(type: "text", nullable: true),
                    ModerationComment = table.Column<string>(type: "text", nullable: true),
                    OriginalMark = table.Column<decimal>(type: "numeric", nullable: true),
                    RevisedMark = table.Column<decimal>(type: "numeric", nullable: true),
                    EntrySource = table.Column<int>(type: "integer", nullable: false),
                    ImportBatchReference = table.Column<string>(type: "text", nullable: true),
                    IsExempt = table.Column<bool>(type: "boolean", nullable: false),
                    ExemptionReason = table.Column<string>(type: "text", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    FeedbackPublished = table.Column<bool>(type: "boolean", nullable: false),
                    GradedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    GradedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_StudentAssessmentMarks", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentAssessmentMarks_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAssessmentMarks_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StudentAssessmentMarks_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAssessmentMarks_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "GradeChangeHistories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentAssessmentMarkId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousScore = table.Column<decimal>(type: "numeric", nullable: false),
                    NewScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PreviousGradeLetter = table.Column<string>(type: "text", nullable: true),
                    NewGradeLetter = table.Column<string>(type: "text", nullable: true),
                    ChangeReason = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ChangedBy = table.Column<string>(type: "text", nullable: true),
                    ChangedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeDetailsJson = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_GradeChangeHistories", x => x.id);
                    table.ForeignKey(
                        name: "FK_GradeChangeHistories_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_GradeChangeHistories_StudentAssessmentMarks_StudentAssessme~",
                        column: x => x.StudentAssessmentMarkId,
                        principalTable: "StudentAssessmentMarks",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_GradeChangeHistories_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_GradeChangeHistories_course_offerings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "course_offerings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_GradeChangeHistories_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "id");
                });

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
                name: "IX_AssessmentExemptions_AssessmentId",
                table: "AssessmentExemptions",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentExemptions_StudentId",
                table: "AssessmentExemptions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_AssessmentTemplateId",
                table: "Assessments",
                column: "AssessmentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_AssessmentTypeId",
                table: "Assessments",
                column: "AssessmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CourseOfferingId",
                table: "Assessments",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_LecturerId",
                table: "Assessments",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_LinkedAssignmentId",
                table: "Assessments",
                column: "LinkedAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SemesterId",
                table: "Assessments",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_UnitId",
                table: "Assessments",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTemplates_AssessmentTypeId",
                table: "AssessmentTemplates",
                column: "AssessmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditLogs_Action",
                table: "CertificateAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditLogs_CertificateId",
                table: "CertificateAuditLogs",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditLogs_CertificateNumber",
                table: "CertificateAuditLogs",
                column: "CertificateNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditLogs_Timestamp",
                table: "CertificateAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditLogs_UserId",
                table: "CertificateAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificateNumber",
                table: "Certificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CourseOfferingId",
                table: "Certificates",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_IssueDate",
                table: "Certificates",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ParentCertificateId",
                table: "Certificates",
                column: "ParentCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Status",
                table: "Certificates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_StudentId",
                table: "Certificates",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_TemplateId",
                table: "Certificates",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_VerificationToken",
                table: "Certificates",
                column: "VerificationToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplates_IsDefault",
                table: "CertificateTemplates",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplates_Name",
                table: "CertificateTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplates_Status",
                table: "CertificateTemplates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplates_Type",
                table: "CertificateTemplates",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignatures_Name_Type",
                table: "DigitalSignatures",
                columns: new[] { "Name", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeBands_GradingScaleId",
                table: "GradeBands",
                column: "GradingScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeChangeHistories_AssessmentId",
                table: "GradeChangeHistories",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeChangeHistories_CourseOfferingId",
                table: "GradeChangeHistories",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeChangeHistories_StudentAssessmentMarkId",
                table: "GradeChangeHistories",
                column: "StudentAssessmentMarkId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeChangeHistories_StudentId",
                table: "GradeChangeHistories",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeChangeHistories_UnitId",
                table: "GradeChangeHistories",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRecords_AssessmentId",
                table: "ModerationRecords",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRecords_CourseOfferingId",
                table: "ModerationRecords",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationRecords_UnitId",
                table: "ModerationRecords",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentMarks_AssessmentId",
                table: "StudentAssessmentMarks",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentMarks_CourseOfferingId",
                table: "StudentAssessmentMarks",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentMarks_EnrollmentId",
                table: "StudentAssessmentMarks",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentMarks_StudentId",
                table: "StudentAssessmentMarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCertificateEligibilities_CertificateRuleId",
                table: "StudentCertificateEligibilities",
                column: "CertificateRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCertificateEligibilities_StudentId",
                table: "StudentCertificateEligibilities",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Titles_Code_Language",
                table: "Titles",
                columns: new[] { "Code", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Titles_IsActive",
                table: "Titles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Titles_NormalizedCode",
                table: "Titles",
                column: "NormalizedCode");

            migrationBuilder.CreateIndex(
                name: "IX_UnitResults_CourseOfferingId",
                table: "UnitResults",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitResults_EnrollmentId",
                table: "UnitResults",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitResults_GradingScaleVersionId",
                table: "UnitResults",
                column: "GradingScaleVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitResults_SemesterId",
                table: "UnitResults",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitResults_StudentId",
                table: "UnitResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitResults_UnitId",
                table: "UnitResults",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentExemptions");

            migrationBuilder.DropTable(
                name: "CertificateAuditLogs");

            migrationBuilder.DropTable(
                name: "DigitalSignatures");

            migrationBuilder.DropTable(
                name: "GradeBands");

            migrationBuilder.DropTable(
                name: "GradeChangeHistories");

            migrationBuilder.DropTable(
                name: "ModerationRecords");

            migrationBuilder.DropTable(
                name: "StudentCertificateEligibilities");

            migrationBuilder.DropTable(
                name: "Titles");

            migrationBuilder.DropTable(
                name: "UnitResults");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "StudentAssessmentMarks");

            migrationBuilder.DropTable(
                name: "CertificateRules");

            migrationBuilder.DropTable(
                name: "GradingScales");

            migrationBuilder.DropTable(
                name: "CertificateTemplates");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "AssessmentTemplates");

            migrationBuilder.DropTable(
                name: "AssessmentTypes");

            migrationBuilder.DropIndex(
                name: "IX_Lecturers_Email",
                table: "Lecturers");

            migrationBuilder.DropIndex(
                name: "IX_Lecturers_EmployeeNumber",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "AspNetUsers");

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
        }
    }
}
