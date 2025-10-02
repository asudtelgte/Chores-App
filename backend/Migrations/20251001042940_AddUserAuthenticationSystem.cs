using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChoreTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthenticationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "UserPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Chores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ChoreCompletions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chores_UserId",
                table: "Chores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoreCompletions_UserId",
                table: "ChoreCompletions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChoreCompletions_Users_UserId",
                table: "ChoreCompletions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chores_Users_UserId",
                table: "Chores",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPreferences_Users_UserId",
                table: "UserPreferences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChoreCompletions_Users_UserId",
                table: "ChoreCompletions");

            migrationBuilder.DropForeignKey(
                name: "FK_Chores_Users_UserId",
                table: "Chores");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_Users_UserId",
                table: "UserPreferences");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_Chores_UserId",
                table: "Chores");

            migrationBuilder.DropIndex(
                name: "IX_ChoreCompletions_UserId",
                table: "ChoreCompletions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Chores");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChoreCompletions");
        }
    }
}
