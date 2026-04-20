using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAppUserStakeholderLinkTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppUserId",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE stakeholders.Stakeholders
                SET AppUserId = links.AppUserId
                FROM stakeholders.Stakeholders stakeholders
                INNER JOIN stakeholders.AppUserStakeholders links
                    ON links.StakeholderId = stakeholders.Id
                WHERE stakeholders.AppUserId IS NULL
                  AND links.IsDeleted = 0;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM stakeholders.Stakeholders
                    WHERE AppUserId IS NULL
                      AND IsDeleted = 0
                )
                BEGIN
                    THROW 50000, 'Unable to migrate Stakeholders.AppUserId because one or more active stakeholders are not linked to an application user.', 1;
                END
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "AppUserId",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholders_AppUserId",
                schema: "stakeholders",
                table: "Stakeholders",
                column: "AppUserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Stakeholders_Users_AppUserId",
                schema: "stakeholders",
                table: "Stakeholders",
                column: "AppUserId",
                principalSchema: "authentication",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.DropTable(
                name: "AppUserStakeholders",
                schema: "stakeholders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stakeholders_Users_AppUserId",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropIndex(
                name: "IX_Stakeholders_AppUserId",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.CreateTable(
                name: "AppUserStakeholders",
                schema: "stakeholders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserStakeholders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserStakeholders_Stakeholders_StakeholderId",
                        column: x => x.StakeholderId,
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserStakeholders_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserStakeholders_AppUserId_StakeholderId",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                columns: new[] { "AppUserId", "StakeholderId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserStakeholders_StakeholderId",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                column: "StakeholderId");
        }
    }
}
