using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Server.Migrations
{
    public partial class ItemReferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayerItem_MatchPlayer_MatchPlayerMatchId_MatchPlayerS~",
                table: "MatchPlayerItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MatchPlayerItem",
                table: "MatchPlayerItem");

            migrationBuilder.DropIndex(
                name: "IX_MatchPlayerItem_MatchPlayerMatchId_MatchPlayerSteamId",
                table: "MatchPlayerItem");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MatchPlayerItem");

            migrationBuilder.DropColumn(
                name: "MatchPlayerMatchId",
                table: "MatchPlayerItem");

            migrationBuilder.DropColumn(
                name: "MatchPlayerSteamId",
                table: "MatchPlayerItem");

            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "MatchPlayerItem",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "MatchId",
                table: "MatchPlayerItem",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "SteamId",
                table: "MatchPlayerItem",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MatchPlayerItem",
                table: "MatchPlayerItem",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerItem_MatchId_SteamId",
                table: "MatchPlayerItem",
                columns: new[] { "MatchId", "SteamId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayerItem_MatchPlayer_MatchId_SteamId",
                table: "MatchPlayerItem",
                columns: new[] { "MatchId", "SteamId" },
                principalTable: "MatchPlayer",
                principalColumns: new[] { "MatchId", "SteamId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayerItem_MatchPlayer_MatchId_SteamId",
                table: "MatchPlayerItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MatchPlayerItem",
                table: "MatchPlayerItem");

            migrationBuilder.DropIndex(
                name: "IX_MatchPlayerItem_MatchId_SteamId",
                table: "MatchPlayerItem");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "MatchPlayerItem");

            migrationBuilder.DropColumn(
                name: "MatchId",
                table: "MatchPlayerItem");

            migrationBuilder.DropColumn(
                name: "SteamId",
                table: "MatchPlayerItem");

            migrationBuilder.AddColumn<decimal>(
                name: "Id",
                table: "MatchPlayerItem",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "MatchPlayerMatchId",
                table: "MatchPlayerItem",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MatchPlayerSteamId",
                table: "MatchPlayerItem",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MatchPlayerItem",
                table: "MatchPlayerItem",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerItem_MatchPlayerMatchId_MatchPlayerSteamId",
                table: "MatchPlayerItem",
                columns: new[] { "MatchPlayerMatchId", "MatchPlayerSteamId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayerItem_MatchPlayer_MatchPlayerMatchId_MatchPlayerS~",
                table: "MatchPlayerItem",
                columns: new[] { "MatchPlayerMatchId", "MatchPlayerSteamId" },
                principalTable: "MatchPlayer",
                principalColumns: new[] { "MatchId", "SteamId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
