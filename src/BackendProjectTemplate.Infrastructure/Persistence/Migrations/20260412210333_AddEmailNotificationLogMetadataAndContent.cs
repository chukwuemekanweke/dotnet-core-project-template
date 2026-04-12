using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationLogMetadataAndContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "NotificationContent",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NotificationType",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryId",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "NotificationContent",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "NotificationType",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "notifications",
                table: "EmailNotificationLogs");
        }
    }
}
