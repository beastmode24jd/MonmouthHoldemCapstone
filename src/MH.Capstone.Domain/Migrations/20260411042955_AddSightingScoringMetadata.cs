using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddSightingScoringMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "loginStreak",
                table: "Sighting",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "rarity",
                table: "Sighting",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "rarityMultipler",
                table: "Sighting",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "totalPointValue",
                table: "Sighting",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "loginStreak",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "rarity",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "rarityMultipler",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "totalPointValue",
                table: "Sighting");
        }
    }
}
