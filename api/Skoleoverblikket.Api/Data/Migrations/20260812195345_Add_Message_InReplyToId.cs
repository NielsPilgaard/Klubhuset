using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Message_InReplyToId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InReplyToId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_InReplyToId",
                table: "Messages",
                column: "InReplyToId",
                filter: "\"InReplyToId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Messages_InReplyToId",
                table: "Messages",
                column: "InReplyToId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_InReplyToId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_InReplyToId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "InReplyToId",
                table: "Messages");
        }
    }
}
