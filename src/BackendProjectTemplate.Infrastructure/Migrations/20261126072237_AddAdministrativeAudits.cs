using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrativeAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auditing");

            migrationBuilder.CreateTable(
                name: "AdministrativeAudits",
                schema: "auditing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorStakeholderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorDisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ActorTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FlowId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Changes = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_AdministrativeAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeAudits_Action_OccurredAtUtc",
                schema: "auditing",
                table: "AdministrativeAudits",
                columns: new[] { "Action", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeAudits_ActorUserId_OccurredAtUtc",
                schema: "auditing",
                table: "AdministrativeAudits",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeAudits_CorrelationId",
                schema: "auditing",
                table: "AdministrativeAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeAudits_OccurredAtUtc_Id",
                schema: "auditing",
                table: "AdministrativeAudits",
                columns: new[] { "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeAudits_TargetTenantId_OccurredAtUtc",
                schema: "auditing",
                table: "AdministrativeAudits",
                columns: new[] { "TargetTenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeAudits_TargetType_TargetId_OccurredAtUtc",
                schema: "auditing",
                table: "AdministrativeAudits",
                columns: new[] { "TargetType", "TargetId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdministrativeAudits",
                schema: "auditing");
        }
    }
}
