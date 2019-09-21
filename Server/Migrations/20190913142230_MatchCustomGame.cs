using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class MatchCustomGame : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomGame",
                table: "Matches",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"Matches\" SET \"CustomGame\" = 'Overthrow'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomGame",
                table: "Matches");
        }
    }
}
