using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAdminPaymentReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookInboxes_MerchantReference",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookInboxes_ProviderReference",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_CreatedAtUtc",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProviderId_MerchantReference_R~",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                columns: new[] { "PaymentProviderId", "MerchantReference", "ReceivedAtUtc", "Id" },
                filter: "\"MerchantReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProviderId_ProviderReference_R~",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                columns: new[] { "PaymentProviderId", "ProviderReference", "ReceivedAtUtc", "Id" },
                filter: "\"ProviderReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CreatedAtUtc_Id",
                schema: "payments",
                table: "PaymentTransactions",
                columns: new[] { "CreatedAtUtc", "Id" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CurrencyId",
                schema: "payments",
                table: "PaymentTransactions",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentMethodType",
                schema: "payments",
                table: "PaymentTransactions",
                column: "PaymentMethodType");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_StakeholderId",
                schema: "payments",
                table: "PaymentTransactions",
                column: "StakeholderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_TenantId",
                schema: "payments",
                table: "PaymentTransactions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProviderId_MerchantReference_R~",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropIndex(
                name: "IX_PaymentWebhookInboxes_PaymentProviderId_ProviderReference_R~",
                schema: "payments",
                table: "PaymentWebhookInboxes");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_CreatedAtUtc_Id",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_CurrencyId",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_PaymentMethodType",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_StakeholderId",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_TenantId",
                schema: "payments",
                table: "PaymentTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookInboxes_MerchantReference",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                column: "MerchantReference",
                filter: "\"MerchantReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookInboxes_ProviderReference",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                column: "ProviderReference",
                filter: "\"ProviderReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CreatedAtUtc",
                schema: "payments",
                table: "PaymentTransactions",
                column: "CreatedAtUtc");
        }
    }
}
