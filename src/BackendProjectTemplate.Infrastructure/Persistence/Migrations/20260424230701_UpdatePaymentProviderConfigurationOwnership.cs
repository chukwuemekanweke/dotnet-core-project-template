using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentProviderConfigurationOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_PaymentProviderConfigurations_PaymentProviders_PaymentProviderId",
                schema: "payments",
                table: "PaymentProviderConfigurations",
                column: "PaymentProviderId",
                principalSchema: "payments",
                principalTable: "PaymentProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentProviderConfigurations_PaymentProviders_PaymentProviderId",
                schema: "payments",
                table: "PaymentProviderConfigurations");
        }
    }
}
