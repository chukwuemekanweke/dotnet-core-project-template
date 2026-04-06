using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStakeholderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stakeholders");

            migrationBuilder.CreateTable(
                name: "StakeholderTypes",
                schema: "stakeholders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakeholderTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stakeholders",
                schema: "stakeholders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StakeholderTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stakeholders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stakeholders_StakeholderTypes_StakeholderTypeId",
                        column: x => x.StakeholderTypeId,
                        principalSchema: "stakeholders",
                        principalTable: "StakeholderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppUserStakeholders",
                schema: "stakeholders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserStakeholders_StakeholderId",
                schema: "stakeholders",
                table: "AppUserStakeholders",
                column: "StakeholderId");

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholders_CountryId",
                schema: "stakeholders",
                table: "Stakeholders",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholders_StakeholderTypeId",
                schema: "stakeholders",
                table: "Stakeholders",
                column: "StakeholderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Stakeholders_TenantId",
                schema: "stakeholders",
                table: "Stakeholders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StakeholderTypes_Key",
                schema: "stakeholders",
                table: "StakeholderTypes",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserStakeholders",
                schema: "stakeholders");

            migrationBuilder.DropTable(
                name: "Stakeholders",
                schema: "stakeholders");

            migrationBuilder.DropTable(
                name: "StakeholderTypes",
                schema: "stakeholders");
        }
    }
}
