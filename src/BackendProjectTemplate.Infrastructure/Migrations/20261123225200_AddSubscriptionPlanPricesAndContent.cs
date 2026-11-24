using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanPricesAndContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlanPrices",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
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
                    table.PrimaryKey("PK_SubscriptionPlanPrices", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPlanPrices_BillingInterval", "\"BillingInterval\" IN (1, 2)");
                    table.CheckConstraint("CK_SubscriptionPlanPrices_Price", "\"Price\" >= 0 AND \"Price\" = trunc(\"Price\")");
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanPrices_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "payments",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanPrices_PlanId_BillingInterval",
                schema: "payments",
                table: "SubscriptionPlanPrices",
                columns: new[] { "PlanId", "BillingInterval" },
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingInterval",
                schema: "payments",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "payments",
                table: "SubscriptionPlans",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Features",
                schema: "payments",
                table: "SubscriptionPlans",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            // Each existing plan was offered on exactly one interval: carry that price into the new table and
            // record it on the subscriptions bought or granted on the plan, before the plan columns are dropped.
            migrationBuilder.Sql("""
                INSERT INTO payments."SubscriptionPlanPrices" (
                    "Id", "PlanId", "BillingInterval", "Price", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "IsDeleted")
                SELECT gen_random_uuid(), p."Id", p."BillingInterval", p."Price", p."CreatedAtUtc", p."CreatedBy",
                       p."UpdatedAtUtc", p."UpdatedBy", FALSE
                FROM payments."SubscriptionPlans" p;

                UPDATE payments."Subscriptions" s
                SET "BillingInterval" = p."BillingInterval"
                FROM payments."SubscriptionPlans" p
                WHERE p."Id" = s."PlanId";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subscriptions_BillingInterval",
                schema: "payments",
                table: "Subscriptions",
                sql: "\"BillingInterval\" IN (1, 2)");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_BillingInterval",
                schema: "payments",
                table: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SubscriptionPlans_Price",
                schema: "payments",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "BillingInterval",
                schema: "payments",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Price",
                schema: "payments",
                table: "SubscriptionPlans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillingInterval",
                schema: "payments",
                table: "SubscriptionPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                schema: "payments",
                table: "SubscriptionPlans",
                type: "numeric(18,0)",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            // Restore a single price per plan, preferring the monthly price when a plan offers several intervals.
            migrationBuilder.Sql("""
                UPDATE payments."SubscriptionPlans" p
                SET "BillingInterval" = chosen."BillingInterval", "Price" = chosen."Price"
                FROM (
                    SELECT DISTINCT ON ("PlanId") "PlanId", "BillingInterval", "Price"
                    FROM payments."SubscriptionPlanPrices"
                    ORDER BY "PlanId", "BillingInterval"
                ) chosen
                WHERE chosen."PlanId" = p."Id";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_BillingInterval",
                schema: "payments",
                table: "SubscriptionPlans",
                sql: "\"BillingInterval\" IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SubscriptionPlans_Price",
                schema: "payments",
                table: "SubscriptionPlans",
                sql: "\"Price\" >= 0 AND \"Price\" = trunc(\"Price\")");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanPrices",
                schema: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subscriptions_BillingInterval",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "BillingInterval",
                schema: "payments",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "payments",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Features",
                schema: "payments",
                table: "SubscriptionPlans");

        }
    }
}
