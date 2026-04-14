using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveProfileFieldsToStakeholderAndAddAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "authentication",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "authentication",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "stakeholders",
                table: "Stakeholders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "stakeholders",
                table: "Stakeholders");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "authentication",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "authentication",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
