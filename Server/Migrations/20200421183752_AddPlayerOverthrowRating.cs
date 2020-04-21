using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using Server.Enums;
using Server.Helpers;
using Server.Models;

namespace Server.Migrations
{
    public partial class AddPlayerOverthrowRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerOverthrowRating",
                columns: table => new
                {
                    MapName = table.Column<string>(nullable: false),
                    PlayerSteamId = table.Column<decimal>(nullable: false),
                    Rating = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerOverthrowRating", x => new { x.PlayerSteamId, x.MapName });
                    table.ForeignKey(
                        name: "FK_PlayerOverthrowRating_Players_PlayerSteamId",
                        column: x => x.PlayerSteamId,
                        principalTable: "Players",
                        principalColumn: "SteamId",
                        onDelete: ReferentialAction.Cascade);
                });

            var sb = new StringBuilder();
            sb.Append("INSERT INTO public.\"PlayerOverthrowRating\" (\"PlayerSteamId\", \"MapName\", \"Rating\") ");
            var flag = false;
            foreach (var mapEnum in Enum.GetValues(typeof(MapEnum)).Cast<MapEnum>())
            {
                if (flag)
                    sb.Append(" UNION ALL ");
                sb.Append($"SELECT public.\"Players\".\"SteamId\", '{ mapEnum.GetDescription() }', { PlayerOverthrowRating.DefaultRating } FROM public.\"Players\"");
                flag = true;
            }

            migrationBuilder.Sql(sb.ToString());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerOverthrowRating");
        }
    }
}
