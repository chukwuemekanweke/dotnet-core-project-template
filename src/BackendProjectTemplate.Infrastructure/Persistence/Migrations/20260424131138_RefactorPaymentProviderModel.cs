using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPaymentProviderModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Providers_ProviderType_IsActive",
                schema: "infrastructure",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProvider_WebhookEventId",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "IntentReference",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ProviderPayloadMetadataJson",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.RenameColumn(
                name: "ProviderId",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                newName: "PaymentProviderId");

            migrationBuilder.RenameColumn(
                name: "ProviderId",
                schema: "payments",
                table: "PaymentTransactions",
                newName: "PaymentProviderId");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderPayloadMetadata",
                schema: "payments",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PaymentProviderConfigurations",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentIntent = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodType = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviders",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ProviderType_IsActive",
                schema: "infrastructure",
                table: "Providers",
                columns: new[] { "ProviderType", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProviderId_WebhookEventId",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                columns: new[] { "PaymentProviderId", "WebhookEventId" },
                unique: true,
                filter: "[WebhookEventId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentProviderId",
                schema: "payments",
                table: "PaymentTransactions",
                column: "PaymentProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderConfigurations_PaymentProviderId_CurrencyId_PaymentIntent",
                schema: "payments",
                table: "PaymentProviderConfigurations",
                columns: new[] { "PaymentProviderId", "CurrencyId", "PaymentIntent" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviders_ProviderKey",
                schema: "payments",
                table: "PaymentProviders",
                column: "ProviderKey",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentProviderConfigurations",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "PaymentProviders",
                schema: "payments");

            migrationBuilder.DropIndex(
                name: "IX_Providers_ProviderType_IsActive",
                schema: "infrastructure",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProviderId_WebhookEventId",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_PaymentProviderId",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "Metadata",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropColumn(
                name: "ProviderPayloadMetadata",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.RenameColumn(
                name: "PaymentProviderId",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                newName: "ProviderId");

            migrationBuilder.RenameColumn(
                name: "PaymentProviderId",
                schema: "payments",
                table: "PaymentTransactions",
                newName: "ProviderId");

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentProvider",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IntentReference",
                schema: "payments",
                table: "PaymentTransactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentProvider",
                schema: "payments",
                table: "PaymentTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPayloadMetadataJson",
                schema: "payments",
                table: "PaymentTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ProviderType_IsActive",
                schema: "infrastructure",
                table: "Providers",
                columns: new[] { "ProviderType", "IsActive" },
                unique: true,
                filter: "[ProviderType] IS NOT NULL AND [ProviderType] <> 3 AND [IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProvider_WebhookEventId",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                columns: new[] { "PaymentProvider", "WebhookEventId" },
                unique: true,
                filter: "[WebhookEventId] IS NOT NULL AND [IsDeleted] = 0");
        }
    }
}
