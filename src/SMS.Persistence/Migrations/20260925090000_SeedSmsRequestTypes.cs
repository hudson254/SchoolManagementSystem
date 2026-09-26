using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SMS.Persistence.Data;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Seeds the OMS request types that thin module adapters depend on, plus the
    /// generic fallback type.
    ///
    /// Design notes:
    ///  * Tenant-safe: one row per active tenant, never a single global row. A
    ///    tenant created later picks these up when its own seed runs; an existing
    ///    tenant is never given another tenant's configuration.
    ///  * Idempotent: ON CONFLICT ("tenant_id","Code") DO NOTHING, so re-running
    ///    the migration is a no-op and operator-applied customisations are never
    ///    overwritten by a deploy.
    ///  * The unique index is (tenant_id, Code); ON CONFLICT targets exactly that.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260925090000_SeedSmsRequestTypes")]
    public partial class SeedSmsRequestTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
INSERT INTO ""sms_request_types""
    (""id"", ""Code"", ""DisplayName"", ""Description"", ""IsActive"",
     ""AllowedRequesterRoles"", ""ApproverRoles"", ""WorkflowSteps"",
     ""RequiresAttachments"", ""EnableComments"", ""DefaultPriority"",
     ""NotifyOnSubmit"", ""NotifyOnApprove"", ""NotifyOnReject"", ""NotifyOnReturn"",
     ""tenant_id"", ""created_at"", ""updated_at"", ""created_date"", ""is_deleted"")
SELECT
    gen_random_uuid(),
    seed.""Code"",
    seed.""DisplayName"",
    seed.""Description"",
    true,
    seed.""AllowedRequesterRoles"",
    seed.""ApproverRoles"",
    seed.""WorkflowSteps"",
    false,
    true,
    1,
    true, true, true, true,
    t.""id"",
    now(), now(), now(), false
FROM ""Tenants"" t
CROSS JOIN (VALUES
    -- ---- Generic fallback (always available) ----
    ('GENERAL_REQUEST', 'General Request',
     'General-purpose request with no module-specific business context. Use the generic OMS request API.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Completed'),

    -- ---- Enrollment module adapter (Phase 3) ----
    ('ENROLLMENT_EXCEPTION', 'Enrollment Exception',
     'Request for an exception relating to an existing enrollment. Raised via the Enrollment module adapter; the Enrollment module remains authoritative for enrollment state.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ENROLLMENT_UNIT_DROP', 'Enrollment Unit Drop',
     'Request to drop or defer a unit the student is enrolled in. The Enrollment module performs the actual change after approval.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ENROLLMENT_COURSE_CHANGE', 'Enrollment Course Change',
     'Request to change a student''s course. The Enrollment module performs the actual change after approval.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ENROLLMENT_UNIT_CORRECTION', 'Enrollment Unit Correction',
     'Request to correct the unit or semester attached to an existing enrollment. The Enrollment module performs the actual change after approval.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ENROLLMENT_CANCELLATION', 'Enrollment Cancellation',
     'Request to cancel an enrollment that has not yet been actioned. The Enrollment module performs the actual change after approval.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),

    -- ---- Accommodation module adapter (Phase 3) ----
    ('ACCOMMODATION_TRANSFER', 'Accommodation Transfer',
     'Request a move to a different house or lane. The Accommodation module performs the actual transfer after approval.',
     'SystemAdministrator,Administrator,Coordinator,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ACCOMMODATION_ALLOCATION', 'Accommodation Allocation',
     'Request a house allocation where none currently exists. The Accommodation module performs the actual allocation after approval.',
     'SystemAdministrator,Administrator,Coordinator,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ACCOMMODATION_EXCEPTION', 'Accommodation Exception',
     'Administrative exception raised against an existing accommodation allocation.',
     'SystemAdministrator,Administrator,Coordinator',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ACCOMMODATION_CORRECTION', 'Accommodation Correction',
     'Request to correct details on an existing accommodation allocation.',
     'SystemAdministrator,Administrator,Coordinator,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),

    -- ---- Assignment module adapter (Phase 3) ----
    ('ASSIGNMENT_EXTENSION', 'Assignment Extension',
     'Request a later deadline for an assignment. The Assignment module performs the actual deadline change after approval.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer,Student',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ASSIGNMENT_REOPEN', 'Assignment Reopen',
     'Request that a closed assignment be reopened for resubmission. The Assignment module performs the actual reopen after approval.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ASSIGNMENT_CORRECTION', 'Assignment Correction',
     'Request a correction to a published assignment''s details. The Assignment module performs the actual change after approval.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed'),
    ('ASSIGNMENT_EXCEPTION', 'Assignment Exception',
     'Administrative exception raised against an assignment.',
     'SystemAdministrator,Administrator,Coordinator,Lecturer',
     'Administrator,Coordinator',
     'Submitted,PendingReview,Approved,Rejected,Returned,Completed')
) AS seed(""Code"", ""DisplayName"", ""Description"", ""AllowedRequesterRoles"", ""ApproverRoles"", ""WorkflowSteps"")
WHERE t.""IsActive"" = true AND t.""is_deleted"" = false
ON CONFLICT (""tenant_id"", ""Code"") DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately a no-op. Rolling back must not delete request types
            // that operators may have customised, nor orphan historical requests
            // that reference these codes.
        }
    }
}