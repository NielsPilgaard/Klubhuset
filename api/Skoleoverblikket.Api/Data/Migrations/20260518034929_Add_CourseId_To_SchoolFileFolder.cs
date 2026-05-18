using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_CourseId_To_SchoolFileFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "SchoolFileFolders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFileFolders_CourseId",
                table: "SchoolFileFolders",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SchoolFileFolders_Courses_CourseId",
                table: "SchoolFileFolders",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SchoolFileFolders_Courses_CourseId",
                table: "SchoolFileFolders");

            migrationBuilder.DropIndex(
                name: "IX_SchoolFileFolders_CourseId",
                table: "SchoolFileFolders");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "SchoolFileFolders");
        }
    }
}
