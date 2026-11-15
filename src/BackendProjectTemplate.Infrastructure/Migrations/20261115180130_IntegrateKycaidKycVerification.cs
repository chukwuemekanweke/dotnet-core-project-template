using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateKycaidKycVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "kyc");

            migrationBuilder.CreateTable(
                name: "KycVerifications",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalApplicantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderApplicantReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderVerificationReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderSessionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VerificationUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FormUrlExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SessionRequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastReconciledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_KycVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KycVerifications_Stakeholders_TenantId_StakeholderId",
                        columns: x => new { x.TenantId, x.StakeholderId },
                        principalSchema: "stakeholders",
                        principalTable: "Stakeholders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KycWebhookInboxes",
                schema: "kyc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VerificationReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SessionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApplicantReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalApplicantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Verified = table.Column<bool>(type: "boolean", nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_KycWebhookInboxes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_ProviderKey_ProviderApplicantReference",
                schema: "kyc",
                table: "KycVerifications",
                columns: new[] { "ProviderKey", "ProviderApplicantReference" });

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_ProviderKey_ProviderVerificationReference",
                schema: "kyc",
                table: "KycVerifications",
                columns: new[] { "ProviderKey", "ProviderVerificationReference" },
                unique: true,
                filter: "\"ProviderVerificationReference\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_TenantId_StakeholderId_StartedAtUtc",
                schema: "kyc",
                table: "KycVerifications",
                columns: new[] { "TenantId", "StakeholderId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_KycVerifications_ActiveStakeholder",
                schema: "kyc",
                table: "KycVerifications",
                columns: new[] { "TenantId", "StakeholderId" },
                unique: true,
                filter: "\"Status\" IN (2, 3) AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_KycWebhookInboxes_ProviderRequest",
                schema: "kyc",
                table: "KycWebhookInboxes",
                columns: new[] { "ProviderKey", "RequestId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KycVerifications",
                schema: "kyc");

            migrationBuilder.DropTable(
                name: "KycWebhookInboxes",
                schema: "kyc");
        }
    }
}
