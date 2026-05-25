using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_SfoWeekPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SfoWeekPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoYear = table.Column<int>(type: "integer", nullable: false),
                    IsoWeek = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SfoWeekPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SfoWeekPlanShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SfoWeekPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SfoShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Beskrivelse = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SfoWeekPlanShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SfoWeekPlanShifts_SfoShifts_SfoShiftId",
                        column: x => x.SfoShiftId,
                        principalTable: "SfoShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SfoWeekPlanShifts_SfoWeekPlans_SfoWeekPlanId",
                        column: x => x.SfoWeekPlanId,
                        principalTable: "SfoWeekPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SfoWeekPlans_TenantId_IsoYear_IsoWeek",
                table: "SfoWeekPlans",
                columns: new[] { "TenantId", "IsoYear", "IsoWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SfoWeekPlanShifts_SfoShiftId",
                table: "SfoWeekPlanShifts",
                column: "SfoShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_SfoWeekPlanShifts_SfoWeekPlanId_SfoShiftId",
                table: "SfoWeekPlanShifts",
                columns: new[] { "SfoWeekPlanId", "SfoShiftId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SfoWeekPlanShifts");

            migrationBuilder.DropTable(
                name: "SfoWeekPlans");
        }
    }
}
