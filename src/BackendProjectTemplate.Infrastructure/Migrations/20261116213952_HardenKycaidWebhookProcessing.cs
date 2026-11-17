using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenKycaidWebhookProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProviderStatus",
                schema: "kyc",
                table: "KycVerifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingOutcome",
                schema: "kyc",
                table: "KycWebhookInboxes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LastProviderTransition",
                schema: "kyc",
                table: "KycVerifications",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedEventPublishedAtUtc",
                schema: "kyc",
                table: "KycVerifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE kyc."KycWebhookInboxes"
                SET "ProcessingOutcome" = 3
                WHERE "ProcessedAtUtc" IS NOT NULL;

                UPDATE kyc."KycVerifications"
                SET "LastProviderTransition" = CASE "Status"
                    WHEN 3 THEN 2
                    WHEN 4 THEN 3
                    WHEN 5 THEN 4
                    ELSE 1
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE kyc."KycVerifications"
                SET "ProviderStatus" = COALESCE(
                    "ProviderStatus",
                    CASE "LastProviderTransition"
                        WHEN 2 THEN 'pending'
                        WHEN 3 THEN 'completed'
                        WHEN 4 THEN 'completed'
                        ELSE 'unknown'
                    END);
                """);

            migrationBuilder.DropColumn(
                name: "ProcessingOutcome",
                schema: "kyc",
                table: "KycWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "LastProviderTransition",
                schema: "kyc",
                table: "KycVerifications");

            migrationBuilder.DropColumn(
                name: "VerifiedEventPublishedAtUtc",
                schema: "kyc",
                table: "KycVerifications");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderStatus",
                schema: "kyc",
                table: "KycVerifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
