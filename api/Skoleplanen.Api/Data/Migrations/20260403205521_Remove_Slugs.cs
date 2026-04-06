using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleplanen.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Slugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schools_Slug",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Schools");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Schools",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Slug",
                table: "Schools",
                column: "Slug",
                unique: true);
        }
    }
}
