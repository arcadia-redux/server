using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class AddPlayerPatreonCosmetics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "PatreonCosmetics",
                table: "Players",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatreonCosmetics",
                table: "Players");
        }
    }
}
