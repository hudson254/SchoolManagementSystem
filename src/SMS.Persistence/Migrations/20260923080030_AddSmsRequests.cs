using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sms_request_number_sequences",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_sms_request_number_sequences", x => new { x.tenant_id, x.Year });
                });

            migrationBuilder.CreateTable(
                name: "sms_request_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowedRequesterRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApproverRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WorkflowSteps = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiresAttachments = table.Column<bool>(type: "boolean", nullable: false),
                    EnableComments = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultPriority = table.Column<int>(type: "integer", nullable: false),
                    NotifyOnSubmit = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnApprove = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnReject = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnReturn = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_sms_request_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sms_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TypeDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequesterUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LecturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccommodationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CertificateId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssignedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousAssigneeUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelatedEntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RejectedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReturnedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReturnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EscalatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EscalatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EscalationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_sms_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sms_request_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EditedMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EditedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_sms_request_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_sms_request_comments_sms_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "sms_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sms_request_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedByUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PerformedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_sms_request_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_sms_request_status_history_sms_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "sms_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_comments_request_id",
                table: "sms_request_comments",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_comments_tenant_id_created_at",
                table: "sms_request_comments",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_comments_tenant_id_request_id",
                table: "sms_request_comments",
                columns: new[] { "tenant_id", "request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_status_history_request_id",
                table: "sms_request_status_history",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_status_history_tenant_id_PerformedAtUtc",
                table: "sms_request_status_history",
                columns: new[] { "tenant_id", "PerformedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_status_history_tenant_id_request_id",
                table: "sms_request_status_history",
                columns: new[] { "tenant_id", "request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_types_tenant_id_Code",
                table: "sms_request_types",
                columns: new[] { "tenant_id", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sms_request_types_tenant_id_IsActive",
                table: "sms_request_types",
                columns: new[] { "tenant_id", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_requests_tenant_id_AssignedUserId",
                table: "sms_requests",
                columns: new[] { "tenant_id", "AssignedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_requests_tenant_id_RequesterUserId",
                table: "sms_requests",
                columns: new[] { "tenant_id", "RequesterUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_requests_tenant_id_RequestNumber",
                table: "sms_requests",
                columns: new[] { "tenant_id", "RequestNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sms_requests_tenant_id_RequestType",
                table: "sms_requests",
                columns: new[] { "tenant_id", "RequestType" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_requests_tenant_id_Status",
                table: "sms_requests",
                columns: new[] { "tenant_id", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sms_request_comments");

            migrationBuilder.DropTable(
                name: "sms_request_number_sequences");

            migrationBuilder.DropTable(
                name: "sms_request_status_history");

            migrationBuilder.DropTable(
                name: "sms_request_types");

            migrationBuilder.DropTable(
                name: "sms_requests");
        }
    }
}
