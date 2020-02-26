using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Server.Migrations
{
    public partial class DropMatchPlayerItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchPlayerItem");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchPlayerItem",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Charges = table.Column<long>(nullable: false),
                    MatchPlayerMatchId = table.Column<long>(nullable: true),
                    MatchPlayerSteamId = table.Column<decimal>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    Slot = table.Column<int>(nullable: false)
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
    }
}
