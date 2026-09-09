using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixHouseOccupantForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Lecturers_OccupantId",
                table: "Houses");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Students_OccupantId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_Houses_OccupantId",
                table: "Houses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Houses_OccupantId",
                table: "Houses",
                column: "OccupantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Lecturers_OccupantId",
                table: "Houses",
                column: "OccupantId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Students_OccupantId",
                table: "Houses",
                column: "OccupantId",
                principalTable: "Students",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
