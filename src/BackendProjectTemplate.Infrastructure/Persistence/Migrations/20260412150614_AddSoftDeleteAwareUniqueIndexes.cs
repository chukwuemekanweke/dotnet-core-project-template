using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAwareUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "authentication",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_BrandKey",
                schema: "stakeholders",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_TenantEmailBaseTemplates_TenantId",
                schema: "notifications",
                table: "TenantEmailBaseTemplates");

            migrationBuilder.DropIndex(
                name: "IX_StakeholderTypes_Key",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.DropIndex(
                name: "IX_EmailProviders_IsActive",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropIndex(
                name: "IX_EmailProviders_ProviderKey",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropIndex(
                name: "IX_EmailNotificationTemplates_NotificationType",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Countries_ShortCode",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_AppUserStakeholders_AppUserId_StakeholderId",
                schema: "stakeholders",
                table: "AppUserStakeholders");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "authentication",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_BrandKey",
                schema: "stakeholders",
                table: "Tenants",
                column: "BrandKey",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TenantEmailBaseTemplates_TenantId",
                schema: "notifications",
                table: "TenantEmailBaseTemplates",
                column: "TenantId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderTypes_Key",
                schema: "stakeholders",
                table: "StakeholderTypes",
                column: "Key",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviders_IsActive",
                schema: "notifications",
                table: "EmailProviders",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviders_ProviderKey",
                schema: "notifications",
                table: "EmailProviders",
                column: "ProviderKey",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationTemplates_NotificationType",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                column: "NotificationType",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_ShortCode",
                schema: "reference_data",
                table: "Countries",
                column: "ShortCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserStakeholders_AppUserId_StakeholderId",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                columns: new[] { "AppUserId", "StakeholderId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                schema: "authentication",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_BrandKey",
                schema: "stakeholders",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_TenantEmailBaseTemplates_TenantId",
                schema: "notifications",
                table: "TenantEmailBaseTemplates");

            migrationBuilder.DropIndex(
                name: "IX_StakeholderTypes_Key",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.DropIndex(
                name: "IX_EmailProviders_IsActive",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropIndex(
                name: "IX_EmailProviders_ProviderKey",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropIndex(
                name: "IX_EmailNotificationTemplates_NotificationType",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Countries_ShortCode",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_AppUserStakeholders_AppUserId_StakeholderId",
                schema: "stakeholders",
                table: "AppUserStakeholders");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "authentication",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_BrandKey",
                schema: "stakeholders",
                table: "Tenants",
                column: "BrandKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantEmailBaseTemplates_TenantId",
                schema: "notifications",
                table: "TenantEmailBaseTemplates",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderTypes_Key",
                schema: "stakeholders",
                table: "StakeholderTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviders_IsActive",
                schema: "notifications",
                table: "EmailProviders",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviders_ProviderKey",
                schema: "notifications",
                table: "EmailProviders",
                column: "ProviderKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotificationTemplates_NotificationType",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                column: "NotificationType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_ShortCode",
                schema: "reference_data",
                table: "Countries",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserStakeholders_AppUserId_StakeholderId",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                columns: new[] { "AppUserId", "StakeholderId" },
                unique: true);
        }
    }
}
