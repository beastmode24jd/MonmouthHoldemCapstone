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
                name: "LoginStreak",
                table: "Sighting",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PointValue",
                table: "Sighting",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Rarity",
                table: "Sighting",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "RarityMultipler",
                table: "Sighting",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginStreak",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "PointValue",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "Rarity",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "RarityMultipler",
                table: "Sighting");
        }
    }
}
