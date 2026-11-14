using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAutomaticSubscriptionRenewal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringPaymentAuthorizations",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AuthorizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_RecurringPaymentAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringPaymentAuthorizations_BillingAccounts_TenantId_Bil~",
                        columns: x => new { x.TenantId, x.BillingAccountId },
                        principalSchema: "payments",
                        principalTable: "BillingAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringPaymentAuthorizations_PaymentProviders_PaymentProv~",
                        column: x => x.PaymentProviderId,
                        principalSchema: "payments",
                        principalTable: "PaymentProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringPaymentAuthorizations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "stakeholders",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionRenewals",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPeriodStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TargetPeriodEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpectedPrice = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRetryAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReminderSequenceSent = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_SubscriptionRenewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewals_BillingAccounts_TenantId_BillingAccoun~",
                        columns: x => new { x.TenantId, x.BillingAccountId },
                        principalSchema: "payments",
                        principalTable: "BillingAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewals_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "payments",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewals_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "payments",
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "stakeholders",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionRenewalAttempts",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RenewalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecurringAuthorizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderIdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRetryAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_SubscriptionRenewalAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewalAttempts_PaymentTransactions_PaymentTran~",
                        column: x => x.PaymentTransactionId,
                        principalSchema: "payments",
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewalAttempts_RecurringPaymentAuthorizations_~",
                        column: x => x.RecurringAuthorizationId,
                        principalSchema: "payments",
                        principalTable: "RecurringPaymentAuthorizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionRenewalAttempts_SubscriptionRenewals_RenewalId",
                        column: x => x.RenewalId,
                        principalSchema: "payments",
                        principalTable: "SubscriptionRenewals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPaymentAuthorizations_BillingAccountId_PaymentProv~",
                schema: "payments",
                table: "RecurringPaymentAuthorizations",
                columns: new[] { "BillingAccountId", "PaymentProviderId", "AuthorizationReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPaymentAuthorizations_PaymentProviderId",
                schema: "payments",
                table: "RecurringPaymentAuthorizations",
                column: "PaymentProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPaymentAuthorizations_TenantId_BillingAccountId_St~",
                schema: "payments",
                table: "RecurringPaymentAuthorizations",
                columns: new[] { "TenantId", "BillingAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewalAttempts_PaymentTransactionId",
                schema: "payments",
                table: "SubscriptionRenewalAttempts",
                column: "PaymentTransactionId",
                unique: true,
                filter: "\"PaymentTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewalAttempts_ProviderIdempotencyKey",
                schema: "payments",
                table: "SubscriptionRenewalAttempts",
                column: "ProviderIdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewalAttempts_RecurringAuthorizationId",
                schema: "payments",
                table: "SubscriptionRenewalAttempts",
                column: "RecurringAuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewalAttempts_RenewalId_AttemptNumber",
                schema: "payments",
                table: "SubscriptionRenewalAttempts",
                columns: new[] { "RenewalId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewals_CurrencyId",
                schema: "payments",
                table: "SubscriptionRenewals",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewals_Status_NextRetryAtUtc",
                schema: "payments",
                table: "SubscriptionRenewals",
                columns: new[] { "Status", "NextRetryAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewals_SubscriptionId_TargetPeriodStartUtc",
                schema: "payments",
                table: "SubscriptionRenewals",
                columns: new[] { "SubscriptionId", "TargetPeriodStartUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRenewals_TenantId_BillingAccountId",
                schema: "payments",
                table: "SubscriptionRenewals",
                columns: new[] { "TenantId", "BillingAccountId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionRenewalAttempts",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "RecurringPaymentAuthorizations",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "SubscriptionRenewals",
                schema: "payments");
        }
    }
}
