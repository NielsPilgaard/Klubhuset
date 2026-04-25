using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleplanen.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_SchoolFileFolder_And_Presign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "SchoolFiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SchoolFileFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolFileFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolFileFolders_SchoolFileFolders_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SchoolFileFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFiles_FolderId",
                table: "SchoolFiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolFileFolders_ParentId",
                table: "SchoolFileFolders",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SchoolFiles_SchoolFileFolders_FolderId",
                table: "SchoolFiles",
                column: "FolderId",
                principalTable: "SchoolFileFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SchoolFiles_SchoolFileFolders_FolderId",
                table: "SchoolFiles");

            migrationBuilder.DropTable(
                name: "SchoolFileFolders");

            migrationBuilder.DropIndex(
                name: "IX_SchoolFiles_FolderId",
                table: "SchoolFiles");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "SchoolFiles");
        }
    }
}
