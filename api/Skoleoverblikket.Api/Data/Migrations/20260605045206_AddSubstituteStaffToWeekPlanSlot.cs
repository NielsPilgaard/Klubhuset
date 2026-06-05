using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubstituteStaffToWeekPlanSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubstituteAideId",
                table: "WeekPlanSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubstituteTeacherId",
                table: "WeekPlanSlots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlanSlots_SubstituteAideId",
                table: "WeekPlanSlots",
                column: "SubstituteAideId");

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlanSlots_SubstituteTeacherId",
                table: "WeekPlanSlots",
                column: "SubstituteTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_WeekPlanSlots_Staff_SubstituteAideId",
                table: "WeekPlanSlots",
                column: "SubstituteAideId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WeekPlanSlots_Staff_SubstituteTeacherId",
                table: "WeekPlanSlots",
                column: "SubstituteTeacherId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeekPlanSlots_Staff_SubstituteAideId",
                table: "WeekPlanSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_WeekPlanSlots_Staff_SubstituteTeacherId",
                table: "WeekPlanSlots");

            migrationBuilder.DropIndex(
                name: "IX_WeekPlanSlots_SubstituteAideId",
                table: "WeekPlanSlots");

            migrationBuilder.DropIndex(
                name: "IX_WeekPlanSlots_SubstituteTeacherId",
                table: "WeekPlanSlots");

            migrationBuilder.DropColumn(
                name: "SubstituteAideId",
                table: "WeekPlanSlots");

            migrationBuilder.DropColumn(
                name: "SubstituteTeacherId",
                table: "WeekPlanSlots");
        }
    }
}
