using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleplanen.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CalendarRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RecurrenceEnd",
                table: "CalendarEntries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceRule",
                table: "CalendarEntries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceEnd",
                table: "CalendarEntries");

            migrationBuilder.DropColumn(
                name: "RecurrenceRule",
                table: "CalendarEntries");
        }
    }
}
