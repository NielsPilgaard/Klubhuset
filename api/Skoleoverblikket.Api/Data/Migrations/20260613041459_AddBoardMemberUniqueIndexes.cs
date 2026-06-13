using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardMemberUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_TenantId_Email",
                table: "BoardMembers",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_TenantId_KeycloakSubject",
                table: "BoardMembers",
                columns: new[] { "TenantId", "KeycloakSubject" },
                unique: true,
                filter: "\"KeycloakSubject\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoardMembers_TenantId_Email",
                table: "BoardMembers");

            migrationBuilder.DropIndex(
                name: "IX_BoardMembers_TenantId_KeycloakSubject",
                table: "BoardMembers");
        }
    }
}
