using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Schemas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Schemas");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Schemas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Schemas",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Schemas");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Schemas");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Schemas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Schemas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
