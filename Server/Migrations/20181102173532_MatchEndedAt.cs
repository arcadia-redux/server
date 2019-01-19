using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Server.Migrations
{
    public partial class MatchEndedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickReason",
                table: "MatchPlayer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MapName",
                table: "Matches",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "Matches",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickReason",
                table: "MatchPlayer");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Matches");

            migrationBuilder.AlterColumn<string>(
                name: "MapName",
                table: "Matches",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
