using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAccountOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillingAccountId",
                schema: "payments",
                table: "SubscriptionActivations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Stakeholders_TenantId_Id",
                schema: "stakeholders",
                table: "Stakeholders",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "BillingAccounts",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAccounts", x => x.Id);
                    table.UniqueConstraint("AK_BillingAccounts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_BillingAccounts_Owner", "(\"OwnerType\" = 1 AND \"StakeholderId\" IS NULL) OR (\"OwnerType\" = 2 AND \"StakeholderId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BillingAccounts_Stakeholders_TenantId_StakeholderId",
                        columns: x => new { x.TenantId, x.StakeholderId },
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "stakeholders",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO payments."BillingAccounts"
                    ("Id", "TenantId", "OwnerType", "StakeholderId", "DisplayName",
                     "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
                SELECT
                    md5(activation."TenantId"::text || ':' || activation."StakeholderId"::text)::uuid,
                    activation."TenantId",
                    2,
                    activation."StakeholderId",
                    left(btrim(stakeholder."FirstName" || ' ' || stakeholder."LastName"), 200),
                    min(activation."CreatedAtUtc"),
                    'billing-account-migration',
                    max(activation."UpdatedAtUtc"),
                    'billing-account-migration',
                    false
                FROM payments."SubscriptionActivations" AS activation
                INNER JOIN stakeholders."Stakeholders" AS stakeholder
                    ON stakeholder."TenantId" = activation."TenantId"
                    AND stakeholder."Id" = activation."StakeholderId"
                GROUP BY
                    activation."TenantId",
                    activation."StakeholderId",
                    stakeholder."FirstName",
                    stakeholder."LastName";

                UPDATE payments."SubscriptionActivations" AS activation
                SET "BillingAccountId" =
                    md5(activation."TenantId"::text || ':' || activation."StakeholderId"::text)::uuid;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM payments."SubscriptionActivations" AS activation
                        LEFT JOIN payments."BillingAccounts" AS account
                            ON account."TenantId" = activation."TenantId"
                            AND account."Id" = activation."BillingAccountId"
                        WHERE account."Id" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Unable to map every subscription activation to a verified billing owner.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "BillingAccountId",
                schema: "payments",
                table: "SubscriptionActivations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionActivations_BillingAccountId",
                schema: "payments",
                table: "SubscriptionActivations",
                column: "BillingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionActivations_TenantId_BillingAccountId",
                schema: "payments",
                table: "SubscriptionActivations",
                columns: new[] { "TenantId", "BillingAccountId" });

            migrationBuilder.CreateIndex(
                name: "UX_BillingAccounts_StakeholderOwner",
                schema: "payments",
                table: "BillingAccounts",
                columns: new[] { "TenantId", "StakeholderId" },
                unique: true,
                filter: "\"OwnerType\" = 2");

            migrationBuilder.CreateIndex(
                name: "UX_BillingAccounts_TenantOwner",
                schema: "payments",
                table: "BillingAccounts",
                column: "TenantId",
                unique: true,
                filter: "\"OwnerType\" = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionActivations_BillingAccounts_TenantId_BillingAcc~",
                schema: "payments",
                table: "SubscriptionActivations",
                columns: new[] { "TenantId", "BillingAccountId" },
                principalSchema: "payments",
                principalTable: "BillingAccounts",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionActivations_BillingAccounts_TenantId_BillingAcc~",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropTable(
                name: "BillingAccounts",
                schema: "payments");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionActivations_BillingAccountId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionActivations_TenantId_BillingAccountId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Stakeholders_TenantId_Id",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "BillingAccountId",
                schema: "payments",
                table: "SubscriptionActivations");
        }
    }
}
