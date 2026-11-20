using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TrackUserDailyActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "UserActivityObservationReceipts",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityObservationReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDailyActivities",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FirstActivityAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActivityCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDailyActivities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityObservationReceipts_ObservationId",
                schema: "analytics",
                table: "UserActivityObservationReceipts",
                column: "ObservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityObservationReceipts_TenantId_StakeholderId_Acti~",
                schema: "analytics",
                table: "UserActivityObservationReceipts",
                columns: new[] { "TenantId", "StakeholderId", "ActivityDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyActivities_TenantId_StakeholderId_ActivityDate",
                schema: "analytics",
                table: "UserDailyActivities",
                columns: new[] { "TenantId", "StakeholderId", "ActivityDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActivityObservationReceipts",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "UserDailyActivities",
                schema: "analytics");
        }
    }
}
