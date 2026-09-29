using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccommodationReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_HouseId",
                table: "AccommodationAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_Houses_TenantId_Status",
                table: "Houses",
                columns: new[] { "tenant_id", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_HouseId_Status",
                table: "AccommodationAssignments",
                columns: new[] { "HouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId_History",
                table: "AccommodationAssignments",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId_History",
                table: "AccommodationAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_TenantId_Status_AssignmentDate",
                table: "AccommodationAssignments",
                columns: new[] { "tenant_id", "Status", "AssignmentDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Houses_TenantId_Status",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_HouseId_Status",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId_History",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId_History",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_TenantId_Status_AssignmentDate",
                table: "AccommodationAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_HouseId",
                table: "AccommodationAssignments",
                column: "HouseId");
        }
    }
}
