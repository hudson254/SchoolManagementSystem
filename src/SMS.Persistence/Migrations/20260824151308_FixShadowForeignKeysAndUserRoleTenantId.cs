using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixShadowForeignKeysAndUserRoleTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Lecturers_LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AccommodationAssignments_Students_StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Departments_DepartmentId1",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropIndex(
                name: "IX_Lecturers_DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropIndex(
                name: "IX_Courses_DepartmentId1",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Lecturers");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LecturerId1",
                table: "AccommodationAssignments");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "AccommodationAssignments");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AspNetUserRoles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUserRoles");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "Lecturers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LecturerId1",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId1",
                table: "AccommodationAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecturers_DepartmentId1",
                table: "Lecturers",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId1",
                table: "Courses",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_LecturerId1",
                table: "AccommodationAssignments",
                column: "LecturerId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationAssignments_StudentId1",
                table: "AccommodationAssignments",
                column: "StudentId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Lecturers_LecturerId1",
                table: "AccommodationAssignments",
                column: "LecturerId1",
                principalTable: "Lecturers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccommodationAssignments_Students_StudentId1",
                table: "AccommodationAssignments",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Departments_DepartmentId1",
                table: "Courses",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturers_Departments_DepartmentId1",
                table: "Lecturers",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "id");
        }
    }
}
