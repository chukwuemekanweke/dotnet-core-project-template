using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentWebhookInboxMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                schema: "payments",
                table: "PaymentWebhookInboxes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                schema: "payments",
                table: "PaymentWebhookInboxes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
