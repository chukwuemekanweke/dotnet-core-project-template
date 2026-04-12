using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditSoftDeleteInterceptorsAndActorMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "Tenants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "Tenants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "notifications",
                table: "TenantEmailBaseTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "notifications",
                table: "TenantEmailBaseTemplates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "notifications",
                table: "TenantEmailBaseTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "notifications",
                table: "TenantEmailBaseTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "notifications",
                table: "TenantEmailBaseTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "StakeholderTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "StakeholderTypes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "StakeholderTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "StakeholderTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "StakeholderTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "integration",
                table: "OutboxMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "integration",
                table: "OutboxMessages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "integration",
                table: "OutboxMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "integration",
                table: "OutboxMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "integration",
                table: "OutboxMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "notifications",
                table: "EmailProviders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "notifications",
                table: "EmailProviders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "notifications",
                table: "EmailProviders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "notifications",
                table: "EmailProviders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "notifications",
                table: "EmailProviders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "notifications",
                table: "EmailNotificationTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "reference_data",
                table: "Countries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "reference_data",
                table: "Countries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "reference_data",
                table: "Countries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "reference_data",
                table: "Countries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "reference_data",
                table: "Countries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "notifications",
                table: "TenantEmailBaseTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "notifications",
                table: "TenantEmailBaseTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "notifications",
                table: "TenantEmailBaseTemplates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "notifications",
                table: "TenantEmailBaseTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "notifications",
                table: "TenantEmailBaseTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "StakeholderTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "notifications",
                table: "EmailProviders");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "notifications",
                table: "EmailNotificationTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "stakeholders",
                table: "AppUserStakeholders");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "stakeholders",
                table: "AppUserStakeholders");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "stakeholders",
                table: "AppUserStakeholders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "stakeholders",
                table: "AppUserStakeholders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "stakeholders",
                table: "AppUserStakeholders");
        }
    }
}
