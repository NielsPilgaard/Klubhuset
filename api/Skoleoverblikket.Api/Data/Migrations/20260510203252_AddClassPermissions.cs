using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassPermissions_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassPermissions_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissions_ClassId",
                table: "ClassPermissions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissions_StaffId",
                table: "ClassPermissions",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassPermissions_TenantId_ClassId_StaffId",
                table: "ClassPermissions",
                columns: new[] { "TenantId", "ClassId", "StaffId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassPermissions");
        }
    }
}
