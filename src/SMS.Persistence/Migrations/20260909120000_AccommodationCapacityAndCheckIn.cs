using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Accommodation repair migration (2026-09-09):
    ///  - Adds multi-occupancy capacity model to Houses (Capacity, OccupiedCount, optional HouseName).
    ///  - Adds explicit CheckInDate/CheckOutDate to AccommodationAssignments.
    ///  - Enforces duplicate-active-assignment prevention with filtered unique indexes.
    ///  - Backfills OccupiedCount/IsOccupied from existing active assignments (data-preserving).
    /// </summary>
    [Migration("20260909120000_AccommodationCapacityAndCheckIn")]
    public partial class AccommodationCapacityAndCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ===== Houses: multi-occupancy capacity model =====
            migrationBuilder.AddColumn<string>(
                name: "HouseName",
                table: "Houses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Houses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OccupiedCount",
                table: "Houses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // ===== AccommodationAssignments: explicit check-in / check-out records =====
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInDate",
                table: "AccommodationAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutDate",
                table: "AccommodationAssignments",
                type: "timestamp with time zone",
                nullable: true);

            // ===== Duplicate-active-assignment prevention (database invariant) =====
            // An occupant may only ever have ONE active ('Active') assignment.
            // These partial unique indexes enforce that invariant regardless of
            // application-layer races or direct database writes.
            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_ActiveStudentId",
                table: "AccommodationAssignments",
                column: "StudentId",
                unique: true,
                filter: "\"StudentId\" IS NOT NULL AND \"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_ActiveLecturerId",
                table: "AccommodationAssignments",
                column: "LecturerId",
                unique: true,
                filter: "\"LecturerId\" IS NOT NULL AND \"Status\" = 'Active'");

            // Legacy Accommodations table: one active record per occupant.
            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_ActiveStudentId",
                table: "Accommodations",
                column: "StudentId",
                unique: true,
                filter: "\"StudentId\" IS NOT NULL AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_ActiveLecturerId",
                table: "Accommodations",
                column: "LecturerId",
                unique: true,
                filter: "\"LecturerId\" IS NOT NULL AND \"IsActive\" = true");

            // ===== Data backfill (preserves existing production accommodation data) =====
            // Derive OccupiedCount/IsOccupied/Status from the authoritative active
            // assignment history instead of guessing from the old single-occupant flag.
            migrationBuilder.Sql(
                @"UPDATE ""Houses"" h
                  SET ""OccupiedCount"" = COALESCE(sub.occ, 0),
                      ""IsOccupied"" = COALESCE(sub.occ, 0) > 0,
                      ""Status"" = CASE WHEN COALESCE(sub.occ, 0) > 0 THEN 'Occupied' ELSE h.""Status"" END,
                      ""updated_at"" = now()
                  FROM (
                      SELECT a.""HouseId"" AS hid, COUNT(*) AS occ
                      FROM ""AccommodationAssignments"" a
                      WHERE a.""Status"" = 'Active' AND a.""is_deleted"" = false
                      GROUP BY a.""HouseId""
                  ) sub
                  WHERE h.""id"" = sub.hid AND h.""is_deleted"" = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accommodations_ActiveLecturerId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_ActiveStudentId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_ActiveLecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_ActiveStudentId",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "CheckOutDate",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "CheckInDate",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "OccupiedCount",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "HouseName",
                table: "Houses");
        }
    }
}