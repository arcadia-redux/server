using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class PlayerPatreonChatWheelFavorites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<int>>(
                name: "PatreonChatWheelFavorites",
                table: "Players",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatreonChatWheelFavorites",
                table: "Players");
        }
    }
}
