using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementAdminUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdministrativelyLockedAtUtc",
                schema: "authentication",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdministrativelyLockedByUserId",
                schema: "authentication",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrativelyLocked",
                schema: "authentication",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AdministrativeSecurityAudits",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FlowId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PreviousRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResultingRoles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AdministrativeSecurityAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsAdministrativelyLocked",
                schema: "authentication",
                table: "Users",
                column: "IsAdministrativelyLocked");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeSecurityAudits_ActorUserId",
                schema: "authentication",
                table: "AdministrativeSecurityAudits",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeSecurityAudits_OccurredAtUtc",
                schema: "authentication",
                table: "AdministrativeSecurityAudits",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeSecurityAudits_TargetUserId",
                schema: "authentication",
                table: "AdministrativeSecurityAudits",
                column: "TargetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdministrativeSecurityAudits",
                schema: "authentication");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsAdministrativelyLocked",
                schema: "authentication",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdministrativelyLockedAtUtc",
                schema: "authentication",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdministrativelyLockedByUserId",
                schema: "authentication",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAdministrativelyLocked",
                schema: "authentication",
                table: "Users");
        }
    }
}
