using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminKycMonitoringIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_KycWebhookInboxes_ProviderKey_SessionReference_ReceivedAtUt~",
                schema: "kyc",
                table: "KycWebhookInboxes",
                columns: new[] { "ProviderKey", "SessionReference", "ReceivedAtUtc", "Id" },
                filter: "\"SessionReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_KycWebhookInboxes_ProviderKey_VerificationReference_Receive~",
                schema: "kyc",
                table: "KycWebhookInboxes",
                columns: new[] { "ProviderKey", "VerificationReference", "ReceivedAtUtc", "Id" },
                filter: "\"VerificationReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_ProviderKey_ProviderSessionReference",
                schema: "kyc",
                table: "KycVerifications",
                columns: new[] { "ProviderKey", "ProviderSessionReference" },
                filter: "\"ProviderSessionReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_StartedAtUtc_Id",
                schema: "kyc",
                table: "KycVerifications",
                columns: new[] { "StartedAtUtc", "Id" },
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KycWebhookInboxes_ProviderKey_SessionReference_ReceivedAtUt~",
                schema: "kyc",
                table: "KycWebhookInboxes");

            migrationBuilder.DropIndex(
                name: "IX_KycWebhookInboxes_ProviderKey_VerificationReference_Receive~",
                schema: "kyc",
                table: "KycWebhookInboxes");

            migrationBuilder.DropIndex(
                name: "IX_KycVerifications_ProviderKey_ProviderSessionReference",
                schema: "kyc",
                table: "KycVerifications");

            migrationBuilder.DropIndex(
                name: "IX_KycVerifications_StartedAtUtc_Id",
                schema: "kyc",
                table: "KycVerifications");
        }
    }
}
