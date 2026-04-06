using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleplanen.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_TimeSlot_SchemaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchemaId",
                table: "TimeSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_SchemaId",
                table: "TimeSlots",
                column: "SchemaId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Schemas_SchemaId",
                table: "TimeSlots",
                column: "SchemaId",
                principalTable: "Schemas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Schemas_SchemaId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_SchemaId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "SchemaId",
                table: "TimeSlots");
        }
    }
}
