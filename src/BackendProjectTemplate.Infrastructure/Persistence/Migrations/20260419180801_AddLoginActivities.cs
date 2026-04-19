using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpAddresses",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    LocationLookupTimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpAddressLocations",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    State = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsCurrentLocation = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAddressLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpAddressLocations_IpAddresses_IpAddressId",
                        column: x => x.IpAddressId,
                        principalSchema: "authentication",
                        principalTable: "IpAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginActivities",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddressLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DevicePlatform = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BrowserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginActivities_IpAddressLocations_IpAddressLocationId",
                        column: x => x.IpAddressLocationId,
                        principalSchema: "authentication",
                        principalTable: "IpAddressLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoginActivities_IpAddresses_IpAddressId",
                        column: x => x.IpAddressId,
                        principalSchema: "authentication",
                        principalTable: "IpAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IpAddresses_LocationLookupTimestampUtc",
                schema: "authentication",
                table: "IpAddresses",
                column: "LocationLookupTimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IpAddresses_Value",
                schema: "authentication",
                table: "IpAddresses",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpAddressLocations_IpAddressId",
                schema: "authentication",
                table: "IpAddressLocations",
                column: "IpAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_IpAddressLocations_IpAddressId_IsCurrentLocation",
                schema: "authentication",
                table: "IpAddressLocations",
                columns: new[] { "IpAddressId", "IsCurrentLocation" },
                unique: true,
                filter: "[IsCurrentLocation] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IpAddressLocations_ResolvedAtUtc",
                schema: "authentication",
                table: "IpAddressLocations",
                column: "ResolvedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_IpAddressId",
                schema: "authentication",
                table: "LoginActivities",
                column: "IpAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_IpAddressLocationId",
                schema: "authentication",
                table: "LoginActivities",
                column: "IpAddressLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_OccurredAtUtc",
                schema: "authentication",
                table: "LoginActivities",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_StakeholderId",
                schema: "authentication",
                table: "LoginActivities",
                column: "StakeholderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_TenantId",
                schema: "authentication",
                table: "LoginActivities",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginActivities",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "IpAddressLocations",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "IpAddresses",
                schema: "authentication");
        }
    }
}
