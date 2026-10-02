using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPriorityActionUrlExpiry : Migration
    {
        /// <summary>
        /// Value written into the new Priority column for pre-existing rows.
        /// </summary>
        private const string BackfillPriority = "Normal";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionUrl",
                table: "Notifications",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            // Added NOT NULL with a real default so the ALTER succeeds against a table
            // that already holds rows. The default matters: EF's own scaffold used "",
            // which is NOT one of the four catalogue priorities. HasUnreadAtOrAbovePriorityAsync
            // matches on the literal catalogue strings, so an empty-string backfill would
            // have made every historical notification permanently invisible to the
            // "unresolved Important/Critical" check the unread badge relies on.
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Notifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: BackfillPriority);

            // Idempotent repair for any row that predates the default, or that was
            // written with an empty/unrecognised priority by an older release.
            migrationBuilder.Sql($@"
UPDATE ""Notifications""
   SET ""Priority"" = '{BackfillPriority}'
 WHERE ""Priority"" IS NULL
    OR ""Priority"" NOT IN ('Informational', 'Normal', 'Important', 'Critical');");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_Priority",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_Priority",
                table: "Notifications");

            // The repair above is destructive to rows written by the new code, so the
            // reverse direction cannot be a mechanical undo of it: the column is simply
            // dropped, which discards the backfilled values along with the column. That
            // is the correct rollback for an additive migration and matches the Down of
            // every other additive migration in this project.
            migrationBuilder.DropColumn(
                name: "ActionUrl",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Notifications");
        }
    }
}
