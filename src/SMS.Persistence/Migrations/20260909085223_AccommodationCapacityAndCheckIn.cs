using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccommodationCapacityAndCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accommodations_LecturerId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_StudentId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Houses",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "HouseName",
                table: "Houses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccupiedCount",
                table: "Houses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_LecturerId",
                table: "Accommodations",
                column: "LecturerId",
                unique: true,
                filter: "\"LecturerId\" IS NOT NULL AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_StudentId",
                table: "Accommodations",
                column: "StudentId",
                unique: true,
                filter: "\"StudentId\" IS NOT NULL AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments",
                column: "LecturerId",
                unique: true,
                filter: "\"LecturerId\" IS NOT NULL AND \"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments",
                column: "StudentId",
                unique: true,
                filter: "\"StudentId\" IS NOT NULL AND \"Status\" = 'Active'");

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
                name: "IX_Accommodations_LecturerId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_StudentId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "HouseName",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "OccupiedCount",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "CheckInDate",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "CheckOutDate",
                table: "AccommodationAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_LecturerId",
                table: "Accommodations",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_StudentId",
                table: "Accommodations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId",
                table: "AccommodationAssignments",
                column: "StudentId");
        }
    }
}
