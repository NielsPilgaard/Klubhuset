using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_GroupMessage_Index_And_Sender_Constraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Messages_GroupMessageId",
                table: "Messages",
                column: "GroupMessageId",
                filter: "\"GroupMessageId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GroupMessages_Sender",
                table: "GroupMessages",
                sql: "\"SenderStaffId\" IS NOT NULL OR \"SenderParentId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_GroupMessageId",
                table: "Messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GroupMessages_Sender",
                table: "GroupMessages");
        }
    }
}
