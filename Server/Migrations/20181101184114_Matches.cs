using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Server.Migrations
{
    public partial class Matches : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Level",
                table: "MatchPlayer",
                nullable: false,
                defaultValue: 0L);

//            migrationBuilder.AlterColumn<long>(
//                name: "MatchId",
//                table: "Matches",
//                nullable: false,
//                oldClrType: typeof(long))
//                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<long>(
                name: "Duration",
                table: "Matches",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "MatchPlayerItem",
                columns: table => new
                {
                    Id = table.Column<decimal>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Charges = table.Column<long>(nullable: false),
                    MatchPlayerMatchId = table.Column<long>(nullable: true),
                    MatchPlayerSteamId = table.Column<decimal>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPlayerItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPlayerItem_MatchPlayer_MatchPlayerMatchId_MatchPlayerS~",
                        columns: x => new { x.MatchPlayerMatchId, x.MatchPlayerSteamId },
                        principalTable: "MatchPlayer",
                        principalColumns: new[] { "MatchId", "SteamId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerItem_MatchPlayerMatchId_MatchPlayerSteamId",
                table: "MatchPlayerItem",
                columns: new[] { "MatchPlayerMatchId", "MatchPlayerSteamId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchPlayerItem");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "MatchPlayer");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Matches");
        }
    }
}
