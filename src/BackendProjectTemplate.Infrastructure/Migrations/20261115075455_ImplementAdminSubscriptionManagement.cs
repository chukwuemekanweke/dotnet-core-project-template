using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAdminSubscriptionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionStatusHistory_NewStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionStatusHistory_PreviousStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subscriptions_Status",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPeriods_ActivationSource",
                schema: "payments",
                table: "SubscriptionPeriods");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.AddColumn<string>(
                name: "Details",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "payments",
                table: "Subscriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EntitlementEndUtc",
                schema: "payments",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EntitlementStartUtc",
                schema: "payments",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FulfillmentBlockedAtUtc",
                schema: "payments",
                table: "SubscriptionPurchases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FulfillmentBlockedReason",
                schema: "payments",
                table: "SubscriptionPurchases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionAllocations",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NominalPrice = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    StartAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AllocationSource = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AllocatedByActorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_SubscriptionAllocations", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionAllocations_AllocationSource", "\"AllocationSource\" IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_SubscriptionAllocations_BillingInterval", "\"BillingInterval\" IN (1, 2)");
                    table.CheckConstraint("CK_SubscriptionAllocations_NominalPrice", "\"NominalPrice\" >= 0 AND \"NominalPrice\" = trunc(\"NominalPrice\")");
                    table.CheckConstraint("CK_SubscriptionAllocations_Window", "\"EndAtUtc\" > \"StartAtUtc\"");
                    table.ForeignKey(
                        name: "FK_SubscriptionAllocations_BillingAccounts_TenantId_BillingAcc~",
                        columns: x => new { x.TenantId, x.BillingAccountId },
                        principalSchema: "payments",
                        principalTable: "BillingAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionAllocations_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "payments",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionAllocations_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "payments",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionAllocations_Subscriptions_TenantId_Subscription~",
                        columns: x => new { x.TenantId, x.SubscriptionId },
                        principalSchema: "payments",
                        principalTable: "Subscriptions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionAllocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "stakeholders",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionStatusHistory_NewStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                sql: "\"NewStatus\" IN (1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionStatusHistory_PreviousStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                sql: "\"PreviousStatus\" IS NULL OR \"PreviousStatus\" IN (1, 2, 3, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedAtUtc_Id",
                schema: "payments",
                table: "Subscriptions",
                columns: new[] { "CreatedAtUtc", "Id" });

            // Backfill the entitlement window for subscriptions that already hold entitlement. A paid subscription
            // that is still renewing automatically holds an open-ended window (NULL upper bound).
            migrationBuilder.Sql(
                """
                UPDATE payments."Subscriptions"
                SET "EntitlementStartUtc" = "CurrentPeriodStartUtc",
                    "EntitlementEndUtc" = CASE
                        WHEN "AutoRenew" AND NOT "CancelAtPeriodEnd" THEN NULL
                        ELSE "CurrentPeriodEndUtc"
                    END
                WHERE "Status" = 2 AND "CurrentPeriodStartUtc" IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subscriptions_EntitlementWindow",
                schema: "payments",
                table: "Subscriptions",
                sql: "(\"Status\" NOT IN (2, 5) OR \"EntitlementStartUtc\" IS NOT NULL) AND (\"EntitlementStartUtc\" IS NULL OR \"EntitlementEndUtc\" IS NULL OR \"EntitlementEndUtc\" >= \"EntitlementStartUtc\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subscriptions_Status",
                schema: "payments",
                table: "Subscriptions",
                sql: "\"Status\" IN (1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPeriods_ActivationSource",
                schema: "payments",
                table: "SubscriptionPeriods",
                sql: "\"ActivationSource\" IN (1, 2, 3)");

            // One active entitlement per BillingAccount at any instant. Scheduled (5) and Active (2) subscriptions
            // reserve the half-open window [EntitlementStartUtc, EntitlementEndUtc); a NULL upper bound is
            // open-ended. Requires btree_gist for the uuid equality operators inside a GiST index. If existing
            // data already violates the rule, this statement fails and the overlap must be resolved first.
            migrationBuilder.Sql(
                """
                ALTER TABLE payments."Subscriptions"
                ADD CONSTRAINT "EX_Subscriptions_BillingAccountEntitlement"
                EXCLUDE USING gist (
                    "TenantId" WITH =,
                    "BillingAccountId" WITH =,
                    tstzrange("EntitlementStartUtc", "EntitlementEndUtc", '[)') WITH &&)
                WHERE ("Status" IN (2, 5));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAllocations_AllocationSource",
                schema: "payments",
                table: "SubscriptionAllocations",
                column: "AllocationSource");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAllocations_CurrencyId",
                schema: "payments",
                table: "SubscriptionAllocations",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAllocations_PlanId",
                schema: "payments",
                table: "SubscriptionAllocations",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAllocations_SubscriptionId",
                schema: "payments",
                table: "SubscriptionAllocations",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAllocations_TenantId_BillingAccountId",
                schema: "payments",
                table: "SubscriptionAllocations",
                columns: new[] { "TenantId", "BillingAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAllocations_TenantId_SubscriptionId",
                schema: "payments",
                table: "SubscriptionAllocations",
                columns: new[] { "TenantId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "UX_SubscriptionAllocations_OperationId",
                schema: "payments",
                table: "SubscriptionAllocations",
                column: "OperationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionAllocations",
                schema: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionStatusHistory_NewStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionStatusHistory_PreviousStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory");

            migrationBuilder.Sql(
                """
                ALTER TABLE payments."Subscriptions"
                DROP CONSTRAINT IF EXISTS "EX_Subscriptions_BillingAccountEntitlement";
                """);

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CreatedAtUtc_Id",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subscriptions_EntitlementWindow",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subscriptions_Status",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPeriods_ActivationSource",
                schema: "payments",
                table: "SubscriptionPeriods");

            migrationBuilder.DropColumn(
                name: "Details",
                schema: "payments",
                table: "SubscriptionStatusHistory");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EntitlementEndUtc",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EntitlementStartUtc",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "FulfillmentBlockedAtUtc",
                schema: "payments",
                table: "SubscriptionPurchases");

            migrationBuilder.DropColumn(
                name: "FulfillmentBlockedReason",
                schema: "payments",
                table: "SubscriptionPurchases");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionStatusHistory_NewStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                sql: "\"NewStatus\" IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionStatusHistory_PreviousStatus",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                sql: "\"PreviousStatus\" IS NULL OR \"PreviousStatus\" IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subscriptions_Status",
                schema: "payments",
                table: "Subscriptions",
                sql: "\"Status\" IN (1, 2, 3, 4)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPeriods_ActivationSource",
                schema: "payments",
                table: "SubscriptionPeriods",
                sql: "\"ActivationSource\" IN (1, 2)");
        }
    }
}
