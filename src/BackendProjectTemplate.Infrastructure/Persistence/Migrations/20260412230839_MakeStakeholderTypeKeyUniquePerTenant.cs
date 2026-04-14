using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeStakeholderTypeKeyUniquePerTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StakeholderTypes_Key",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderTypes_TenantId_Key",
                schema: "stakeholders",
                table: "StakeholderTypes",
                columns: new[] { "TenantId", "Key" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StakeholderTypes_TenantId_Key",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderTypes_Key",
                schema: "stakeholders",
                table: "StakeholderTypes",
                column: "Key",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
