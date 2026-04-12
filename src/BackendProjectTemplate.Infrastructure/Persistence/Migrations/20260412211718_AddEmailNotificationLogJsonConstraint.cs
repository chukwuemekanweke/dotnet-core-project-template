using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationLogJsonConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailNotificationLogs_NotificationContent_IsJson",
                schema: "notifications",
                table: "EmailNotificationLogs",
                sql: "ISJSON([NotificationContent]) = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailNotificationLogs_NotificationContent_IsJson",
                schema: "notifications",
                table: "EmailNotificationLogs");
        }
    }
}
