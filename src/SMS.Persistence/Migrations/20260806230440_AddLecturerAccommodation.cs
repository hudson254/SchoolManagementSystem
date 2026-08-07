using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturerAccommodation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OccupantType",
                table: "Houses",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "Accommodations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "LecturerId",
                table: "Accommodations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccupantType",
                table: "Accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "LecturerId",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LecturerId1",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccupantType",
                table: "AccommodationAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_LecturerId",
                table: "Accommodations",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments",
                column: "LecturerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments",
                column: "LecturerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Lecturers_LecturerId",
                table: "AccommodationAssignments",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Lecturers_LecturerId1",
                table: "AccommodationAssignments",
                column: "LecturerId1",
                principalTable: "Lecturers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_Lecturers_LecturerId",
                table: "Accommodations",
                column: "LecturerId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_Lecturers_OccupantId",
                table: "Houses",
                column: "OccupantId",
                principalTable: "Lecturers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Lecturers_LecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Lecturers_LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_Lecturers_LecturerId",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_Houses_Lecturers_OccupantId",
                table: "Houses");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_LecturerId",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "OccupantType",
                table: "Houses");

            migrationBuilder.DropColumn(
                name: "LecturerId",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "OccupantType",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "LecturerId",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "OccupantType",
                table: "AccommodationAssignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "Accommodations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
