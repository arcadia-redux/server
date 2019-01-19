using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Server.Migrations
{
    public partial class Patreon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PatreonBootsEnabled",
                table: "Players",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatreonEmblemColor",
                table: "Players",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PatreonEmblemEnabled",
                table: "Players",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatreonBootsEnabled",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PatreonEmblemColor",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PatreonEmblemEnabled",
                table: "Players");
        }
    }
}
