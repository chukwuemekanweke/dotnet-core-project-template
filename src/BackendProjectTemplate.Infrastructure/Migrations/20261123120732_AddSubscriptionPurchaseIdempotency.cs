using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPurchaseIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPurchases_TenantId_InitiatedByStakeholderId",
                schema: "payments",
                table: "SubscriptionPurchases");

            migrationBuilder.AddColumn<Guid>(
                name: "OperationId",
                schema: "payments",
                table: "SubscriptionPurchases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                schema: "payments",
                table: "SubscriptionPurchases",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE payments."SubscriptionPurchases"
                SET "OperationId" = "Id",
                    "RequestFingerprint" = upper(replace("Id"::text, '-', ''));
                """);

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionPurchases_TenantId_InitiatedByStakeholderId_OperationId",
                schema: "payments",
                table: "SubscriptionPurchases",
                columns: new[] { "TenantId", "InitiatedByStakeholderId", "OperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SubscriptionPurchases_TenantId_InitiatedByStakeholderId_OperationId",
                schema: "payments",
                table: "SubscriptionPurchases");

            migrationBuilder.DropColumn(
                name: "OperationId",
                schema: "payments",
                table: "SubscriptionPurchases");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                schema: "payments",
                table: "SubscriptionPurchases");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchases_TenantId_InitiatedByStakeholderId",
                schema: "payments",
                table: "SubscriptionPurchases",
                columns: new[] { "TenantId", "InitiatedByStakeholderId" });
        }
    }
}
