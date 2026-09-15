using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadFileLinkToLectureNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UploadFileId",
                table: "LectureNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectureNotes_UploadFileId",
                table: "LectureNotes",
                column: "UploadFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_LectureNotes_upload_files_UploadFileId",
                table: "LectureNotes",
                column: "UploadFileId",
                principalTable: "upload_files",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LectureNotes_upload_files_UploadFileId",
                table: "LectureNotes");

            migrationBuilder.DropIndex(
                name: "IX_LectureNotes_UploadFileId",
                table: "LectureNotes");

            migrationBuilder.DropColumn(
                name: "UploadFileId",
                table: "LectureNotes");
        }
    }
}
