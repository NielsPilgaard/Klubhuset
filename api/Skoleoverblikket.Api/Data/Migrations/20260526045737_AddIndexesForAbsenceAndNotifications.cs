using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skoleoverblikket.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForAbsenceAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_RecipientId_CreatedAt",
                table: "Notifications",
                columns: new[] { "TenantId", "RecipientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_RecipientId_ReadAt",
                table: "Notifications",
                columns: new[] { "TenantId", "RecipientId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_TenantId_UserId_UserType_Type",
                table: "NotificationPreferences",
                columns: new[] { "TenantId", "UserId", "UserType", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceReports_TenantId_Date",
                table: "AbsenceReports",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceReports_TenantId_Status",
                table: "AbsenceReports",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId_RecipientId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId_RecipientId_ReadAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_TenantId_UserId_UserType_Type",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceReports_TenantId_Date",
                table: "AbsenceReports");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceReports_TenantId_Status",
                table: "AbsenceReports");
        }
    }
}
