using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementSubscriptionDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubscriptionActivations_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                schema: "payments",
                table: "SubscriptionActivations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPeriodId",
                schema: "payments",
                table: "SubscriptionActivations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPlans_BillingInterval", "\"BillingInterval\" IN (1, 2)");
                    table.CheckConstraint("CK_SubscriptionPlans_Price", "\"Price\" >= 0 AND \"Price\" = trunc(\"Price\")");
                    table.ForeignKey(
                        name: "FK_SubscriptionPlans_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "payments",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentPeriodStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextBillingAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationRequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSuccessfulPaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.UniqueConstraint("AK_Subscriptions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Subscriptions_CurrentPeriod", "\"CurrentPeriodStartUtc\" IS NULL OR \"CurrentPeriodEndUtc\" > \"CurrentPeriodStartUtc\"");
                    table.CheckConstraint("CK_Subscriptions_Status", "\"Status\" IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_Subscriptions_BillingAccounts_TenantId_BillingAccountId",
                        columns: x => new { x.TenantId, x.BillingAccountId },
                        principalSchema: "payments",
                        principalTable: "BillingAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_PaymentTransactions_LastSuccessfulPaymentTran~",
                        column: x => x.LastSuccessfulPaymentTransactionId,
                        principalSchema: "payments",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "payments",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "stakeholders",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPeriods",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivationSource = table.Column<int>(type: "integer", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SubscriptionPeriods", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPeriods_ActivationSource", "\"ActivationSource\" IN (1, 2)");
                    table.CheckConstraint("CK_SubscriptionPeriods_BillingInterval", "\"BillingInterval\" IN (1, 2)");
                    table.CheckConstraint("CK_SubscriptionPeriods_Boundaries", "\"PeriodEndUtc\" > \"PeriodStartUtc\"");
                    table.CheckConstraint("CK_SubscriptionPeriods_Price", "\"Price\" >= 0 AND \"Price\" = trunc(\"Price\")");
                    table.ForeignKey(
                        name: "FK_SubscriptionPeriods_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "payments",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPeriods_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "payments",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPeriods_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "payments",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPeriods_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "payments",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPurchases",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    ExpectedCurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatedByStakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfilledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_SubscriptionPurchases", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPurchases_BillingInterval", "\"BillingInterval\" IN (1, 2)");
                    table.CheckConstraint("CK_SubscriptionPurchases_ExpectedAmount", "\"ExpectedAmount\" > 0 AND \"ExpectedAmount\" = trunc(\"ExpectedAmount\")");
                    table.ForeignKey(
                        name: "FK_SubscriptionPurchases_BillingAccounts_TenantId_BillingAccou~",
                        columns: x => new { x.TenantId, x.BillingAccountId },
                        principalSchema: "payments",
                        principalTable: "BillingAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPurchases_Currencies_ExpectedCurrencyId",
                        column: x => x.ExpectedCurrencyId,
                        principalSchema: "payments",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPurchases_PaymentTransactions_PaymentTransactio~",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "payments",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPurchases_Stakeholders_TenantId_InitiatedByStak~",
                        columns: x => new { x.TenantId, x.InitiatedByStakeholderId },
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPurchases_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "payments",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPurchases_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "payments",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPurchases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "stakeholders",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionStatusHistory",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_SubscriptionStatusHistory", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionStatusHistory_NewStatus", "\"NewStatus\" IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_SubscriptionStatusHistory_PreviousStatus", "\"PreviousStatus\" IS NULL OR \"PreviousStatus\" IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_SubscriptionStatusHistory_PaymentTransactions_PaymentTransa~",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "payments",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionStatusHistory_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "payments",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionActivations_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionActivations",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionActivations_SubscriptionId",
                schema: "payments",
                table: "SubscriptionActivations",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionActivations_SubscriptionPeriodId",
                schema: "payments",
                table: "SubscriptionActivations",
                column: "SubscriptionPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPeriods_CurrencyId",
                schema: "payments",
                table: "SubscriptionPeriods",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPeriods_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionPeriods",
                column: "PaymentTransactionId",
                unique: true,
                filter: "\"PaymentTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPeriods_PlanId",
                schema: "payments",
                table: "SubscriptionPeriods",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPeriods_SubscriptionId_OperationId",
                schema: "payments",
                table: "SubscriptionPeriods",
                columns: new[] { "SubscriptionId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPeriods_SubscriptionId_PeriodStartUtc",
                schema: "payments",
                table: "SubscriptionPeriods",
                columns: new[] { "SubscriptionId", "PeriodStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                schema: "payments",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_CurrencyId",
                schema: "payments",
                table: "SubscriptionPlans",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsActive",
                schema: "payments",
                table: "SubscriptionPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchases_ExpectedCurrencyId",
                schema: "payments",
                table: "SubscriptionPurchases",
                column: "ExpectedCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchases_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionPurchases",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchases_PlanId",
                schema: "payments",
                table: "SubscriptionPurchases",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchases_SubscriptionId",
                schema: "payments",
                table: "SubscriptionPurchases",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchases_TenantId_BillingAccountId",
                schema: "payments",
                table: "SubscriptionPurchases",
                columns: new[] { "TenantId", "BillingAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPurchases_TenantId_InitiatedByStakeholderId",
                schema: "payments",
                table: "SubscriptionPurchases",
                columns: new[] { "TenantId", "InitiatedByStakeholderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_LastSuccessfulPaymentTransactionId",
                schema: "payments",
                table: "Subscriptions",
                column: "LastSuccessfulPaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                schema: "payments",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status_NextBillingAtUtc",
                schema: "payments",
                table: "Subscriptions",
                columns: new[] { "Status", "NextBillingAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_BillingAccountId_Status",
                schema: "payments",
                table: "Subscriptions",
                columns: new[] { "TenantId", "BillingAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionStatusHistory_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionStatusHistory_SubscriptionId_OccurredAtUtc",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                columns: new[] { "SubscriptionId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionStatusHistory_SubscriptionId_OperationId",
                schema: "payments",
                table: "SubscriptionStatusHistory",
                columns: new[] { "SubscriptionId", "OperationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionActivations_SubscriptionPeriods_SubscriptionPer~",
                schema: "payments",
                table: "SubscriptionActivations",
                column: "SubscriptionPeriodId",
                principalSchema: "payments",
                principalTable: "SubscriptionPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionActivations_Subscriptions_SubscriptionId",
                schema: "payments",
                table: "SubscriptionActivations",
                column: "SubscriptionId",
                principalSchema: "payments",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionActivations_SubscriptionPeriods_SubscriptionPer~",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionActivations_Subscriptions_SubscriptionId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropTable(
                name: "SubscriptionPeriods",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "SubscriptionPurchases",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "SubscriptionStatusHistory",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans",
                schema: "payments");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionActivations_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionActivations_SubscriptionId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionActivations_SubscriptionPeriodId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.DropColumn(
                name: "SubscriptionPeriodId",
                schema: "payments",
                table: "SubscriptionActivations");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionActivations_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionActivations",
                column: "PaymentTransactionId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
