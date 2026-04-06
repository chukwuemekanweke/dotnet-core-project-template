using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendProjectTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryCallingCodeAndFlagUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Code",
                schema: "reference_data",
                table: "Countries",
                newName: "ShortCode");

            migrationBuilder.RenameIndex(
                name: "IX_Countries_Code",
                schema: "reference_data",
                table: "Countries",
                newName: "IX_Countries_ShortCode");

            migrationBuilder.AddColumn<string>(
                name: "CallingCode",
                schema: "reference_data",
                table: "Countries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagUrl",
                schema: "reference_data",
                table: "Countries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CallingCode",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "FlagUrl",
                schema: "reference_data",
                table: "Countries");

            migrationBuilder.RenameColumn(
                name: "ShortCode",
                schema: "reference_data",
                table: "Countries",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_Countries_ShortCode",
                schema: "reference_data",
                table: "Countries",
                newName: "IX_Countries_Code");
        }
    }
}
