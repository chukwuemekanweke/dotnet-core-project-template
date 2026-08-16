using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeFileUploadSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "infrastructure");

            migrationBuilder.DropForeignKey(
                name: "FK_AvatarUploads_Stakeholders_StakeholderId",
                schema: "stakeholders",
                table: "AvatarUploads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvatarUploads",
                schema: "stakeholders",
                table: "AvatarUploads");

            migrationBuilder.DropIndex(
                name: "IX_AvatarUploads_StakeholderId",
                schema: "stakeholders",
                table: "AvatarUploads");

            migrationBuilder.RenameTable(
                name: "AvatarUploads",
                schema: "stakeholders",
                newName: "FileUploadSessions",
                newSchema: "infrastructure");

            migrationBuilder.RenameColumn(
                name: "StakeholderId",
                schema: "infrastructure",
                table: "FileUploadSessions",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "FinalUrl",
                schema: "infrastructure",
                table: "FileUploadSessions",
                newName: "FinalLocation");

            migrationBuilder.RenameIndex(
                name: "IX_AvatarUploads_Status_ExpiresAtUtc",
                schema: "infrastructure",
                table: "FileUploadSessions",
                newName: "IX_FileUploadSessions_Status_ExpiresAtUtc");

            migrationBuilder.AddColumn<int>(
                name: "DestinationVisibility",
                schema: "infrastructure",
                table: "FileUploadSessions",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "InitiatedByStakeholderId",
                schema: "infrastructure",
                table: "FileUploadSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerType",
                schema: "infrastructure",
                table: "FileUploadSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "stakeholder");

            migrationBuilder.AddColumn<string>(
                name: "PolicyKey",
                schema: "infrastructure",
                table: "FileUploadSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "stakeholder-avatar-v1");

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                schema: "infrastructure",
                table: "FileUploadSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "stakeholder-profile-avatar");

            migrationBuilder.Sql(
                """
                UPDATE infrastructure."FileUploadSessions"
                SET "InitiatedByStakeholderId" = "OwnerId";

                ALTER TABLE infrastructure."FileUploadSessions"
                    ALTER COLUMN "DestinationVisibility" DROP DEFAULT,
                    ALTER COLUMN "OwnerType" DROP DEFAULT,
                    ALTER COLUMN "PolicyKey" DROP DEFAULT,
                    ALTER COLUMN "Purpose" DROP DEFAULT;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileUploadSessions",
                schema: "infrastructure",
                table: "FileUploadSessions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadSessions_TenantId_OwnerType_OwnerId_Purpose",
                schema: "infrastructure",
                table: "FileUploadSessions",
                columns: new[] { "TenantId", "OwnerType", "OwnerId", "Purpose" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FileUploadSessions",
                schema: "infrastructure",
                table: "FileUploadSessions");

            migrationBuilder.DropIndex(
                name: "IX_FileUploadSessions_TenantId_OwnerType_OwnerId_Purpose",
                schema: "infrastructure",
                table: "FileUploadSessions");

            migrationBuilder.DropColumn(
                name: "DestinationVisibility",
                schema: "infrastructure",
                table: "FileUploadSessions");

            migrationBuilder.DropColumn(
                name: "InitiatedByStakeholderId",
                schema: "infrastructure",
                table: "FileUploadSessions");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                schema: "infrastructure",
                table: "FileUploadSessions");

            migrationBuilder.DropColumn(
                name: "PolicyKey",
                schema: "infrastructure",
                table: "FileUploadSessions");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "infrastructure",
                table: "FileUploadSessions");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                schema: "infrastructure",
                table: "FileUploadSessions",
                newName: "StakeholderId");

            migrationBuilder.RenameColumn(
                name: "FinalLocation",
                schema: "infrastructure",
                table: "FileUploadSessions",
                newName: "FinalUrl");

            migrationBuilder.RenameIndex(
                name: "IX_FileUploadSessions_Status_ExpiresAtUtc",
                schema: "infrastructure",
                table: "FileUploadSessions",
                newName: "IX_AvatarUploads_Status_ExpiresAtUtc");

            migrationBuilder.RenameTable(
                name: "FileUploadSessions",
                schema: "infrastructure",
                newName: "AvatarUploads",
                newSchema: "stakeholders");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvatarUploads",
                schema: "stakeholders",
                table: "AvatarUploads",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AvatarUploads_Stakeholders_StakeholderId",
                schema: "stakeholders",
                table: "AvatarUploads",
                column: "StakeholderId",
                principalSchema: "stakeholders",
                principalTable: "Stakeholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_AvatarUploads_StakeholderId",
                schema: "stakeholders",
                table: "AvatarUploads",
                column: "StakeholderId");
        }
    }
}
