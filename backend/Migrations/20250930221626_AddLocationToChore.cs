using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoreTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToChore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Chores",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Chores");
        }
    }
}
