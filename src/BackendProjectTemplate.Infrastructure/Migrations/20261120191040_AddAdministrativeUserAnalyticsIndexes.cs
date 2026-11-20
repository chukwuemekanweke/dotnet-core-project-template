using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrativeUserAnalyticsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserDailyActivities_TenantId_ActivityDate_StakeholderId",
                schema: "analytics",
                table: "UserDailyActivities",
                columns: new[] { "TenantId", "ActivityDate", "StakeholderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholders_TenantId_CreatedAtUtc_Id",
                schema: "stakeholders",
                table: "Stakeholders",
                columns: new[] { "TenantId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_TenantId_StakeholderId_ActivityType_Occurre~",
                schema: "authentication",
                table: "LoginActivities",
                columns: new[] { "TenantId", "StakeholderId", "ActivityType", "OccurredAtUtc" },
                descending: new[] { false, false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserDailyActivities_TenantId_ActivityDate_StakeholderId",
                schema: "analytics",
                table: "UserDailyActivities");

            migrationBuilder.DropIndex(
                name: "IX_Stakeholders_TenantId_CreatedAtUtc_Id",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropIndex(
                name: "IX_LoginActivities_TenantId_StakeholderId_ActivityType_Occurre~",
                schema: "authentication",
                table: "LoginActivities");
        }
    }
}
