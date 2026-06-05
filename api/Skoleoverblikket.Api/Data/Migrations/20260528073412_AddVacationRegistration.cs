using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVacationRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VacationRegistrationWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegistrationDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                    CareStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CareEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Granularity = table.Column<int>(type: "integer", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationRegistrationWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VacationRegistrationEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WindowId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByParentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedDates = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationRegistrationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacationRegistrationEntries_Parents_SubmittedByParentId",
                        column: x => x.SubmittedByParentId,
                        principalTable: "Parents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VacationRegistrationEntries_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VacationRegistrationEntries_VacationRegistrationWindows_Win~",
                        column: x => x.WindowId,
                        principalTable: "VacationRegistrationWindows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VacationRegistrationEntries_StudentId",
                table: "VacationRegistrationEntries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRegistrationEntries_SubmittedByParentId",
                table: "VacationRegistrationEntries",
                column: "SubmittedByParentId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRegistrationEntries_TenantId_WindowId_StudentId",
                table: "VacationRegistrationEntries",
                columns: new[] { "TenantId", "WindowId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacationRegistrationEntries_WindowId",
                table: "VacationRegistrationEntries",
                column: "WindowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VacationRegistrationEntries");

            migrationBuilder.DropTable(
                name: "VacationRegistrationWindows");
        }
    }
}
