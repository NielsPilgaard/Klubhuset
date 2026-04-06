using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleplanen.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_WeekPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeekPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoYear = table.Column<int>(type: "integer", nullable: false),
                    IsoWeek = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeekPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeekPlans_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeekPlanSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Beskrivelse = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Lektier = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    FagSwapCourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeekPlanSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeekPlanSlots_Courses_FagSwapCourseId",
                        column: x => x.FagSwapCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WeekPlanSlots_SchemaSlots_SchemaSlotId",
                        column: x => x.SchemaSlotId,
                        principalTable: "SchemaSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeekPlanSlots_WeekPlans_WeekPlanId",
                        column: x => x.WeekPlanId,
                        principalTable: "WeekPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeekPlanSlotFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekPlanSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolFileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeekPlanSlotFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeekPlanSlotFiles_SchoolFiles_SchoolFileId",
                        column: x => x.SchoolFileId,
                        principalTable: "SchoolFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeekPlanSlotFiles_WeekPlanSlots_WeekPlanSlotId",
                        column: x => x.WeekPlanSlotId,
                        principalTable: "WeekPlanSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlans_ClassId",
                table: "WeekPlans",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlans_TenantId_ClassId_IsoYear_IsoWeek",
                table: "WeekPlans",
                columns: new[] { "TenantId", "ClassId", "IsoYear", "IsoWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlanSlotFiles_SchoolFileId",
                table: "WeekPlanSlotFiles",
                column: "SchoolFileId");

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlanSlotFiles_WeekPlanSlotId_SchoolFileId",
                table: "WeekPlanSlotFiles",
                columns: new[] { "WeekPlanSlotId", "SchoolFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlanSlots_FagSwapCourseId",
                table: "WeekPlanSlots",
                column: "FagSwapCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlanSlots_SchemaSlotId",
                table: "WeekPlanSlots",
                column: "SchemaSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_WeekPlanSlots_WeekPlanId_SchemaSlotId",
                table: "WeekPlanSlots",
                columns: new[] { "WeekPlanId", "SchemaSlotId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeekPlanSlotFiles");

            migrationBuilder.DropTable(
                name: "WeekPlanSlots");

            migrationBuilder.DropTable(
                name: "WeekPlans");
        }
    }
}
