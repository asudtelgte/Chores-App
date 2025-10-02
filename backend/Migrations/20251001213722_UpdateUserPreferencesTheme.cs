using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoreTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserPreferencesTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "UserPreferences",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "UserPreferences");
        }
    }
}
