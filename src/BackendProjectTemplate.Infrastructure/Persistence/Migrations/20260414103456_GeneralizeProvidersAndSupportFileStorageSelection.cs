using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeProvidersAndSupportFileStorageSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "infrastructure");

            migrationBuilder.DropIndex(
                name: "IX_EmailProviders_IsActive",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropIndex(
                name: "IX_EmailProviders_ProviderKey",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailProviders",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.RenameTable(
                name: "EmailProviders",
                schema: "notifications",
                newName: "Providers",
                newSchema: "infrastructure");

            migrationBuilder.AddColumn<int>(
                name: "ProviderType",
                schema: "infrastructure",
                table: "Providers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Providers",
                schema: "infrastructure",
                table: "Providers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ProviderType_IsActive",
                schema: "infrastructure",
                table: "Providers",
                columns: new[] { "ProviderType", "IsActive" },
                unique: true,
                filter: "[ProviderType] IS NOT NULL AND [IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ProviderType_ProviderKey",
                schema: "infrastructure",
                table: "Providers",
                columns: new[] { "ProviderType", "ProviderKey" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Providers_ProviderType_IsActive",
                schema: "infrastructure",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Providers_ProviderType_ProviderKey",
                schema: "infrastructure",
                table: "Providers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Providers",
                schema: "infrastructure",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ProviderType",
                schema: "infrastructure",
                table: "Providers");

            migrationBuilder.RenameTable(
                name: "Providers",
                schema: "infrastructure",
                newName: "EmailProviders",
                newSchema: "notifications");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailProviders",
                schema: "notifications",
                table: "EmailProviders",
                column: "Id");

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
        }
    }
}
