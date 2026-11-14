using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteMailtrapDeliveryWebhookIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryFailureReason",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryStatus",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastDeliveryEventAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE notifications."EmailNotificationLogs"
                SET "DeliveryStatus" = CASE
                    WHEN "DeliveredAtUtc" IS NOT NULL THEN 3
                    WHEN "SentAtUtc" IS NOT NULL THEN 2
                    ELSE 1
                END,
                "LastDeliveryEventAtUtc" = "DeliveredAtUtc";

                UPDATE notifications."EmailDeliveryWebhookInboxes"
                SET "EventType" = 'delivery';
                """);

            migrationBuilder.AlterColumn<int>(
                name: "DeliveryStatus",
                schema: "notifications",
                table: "EmailNotificationLogs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryFailureReason",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "FailedAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "LastDeliveryEventAtUtc",
                schema: "notifications",
                table: "EmailNotificationLogs");

            migrationBuilder.DropColumn(
                name: "EventType",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "notifications",
                table: "EmailDeliveryWebhookInboxes");
        }
    }
}
