using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class AddPlayerOverthrowRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Dictionary<string, int>>(
                name: "RatingOverthrow",
                table: "Players",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{\"mines_trio\":2000,\"desert_duo\":2000,\"forest_solo\":2000,\"desert_quintet\":2000,\"temple_quartet\":2000,\"desert_octet\":2000,\"temple_sextet\":2000,\"core_quartet\":2000}'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RatingOverthrow",
                table: "Players");
        }
    }
}
