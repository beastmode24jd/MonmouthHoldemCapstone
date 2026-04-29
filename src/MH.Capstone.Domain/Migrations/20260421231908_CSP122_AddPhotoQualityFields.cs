using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class CSP122_AddPhotoQualityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FlaggedForReview",
                table: "Sighting",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "LuminanceAverage",
                table: "Sighting",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityTier",
                table: "Sighting",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResolutionHeight",
                table: "Sighting",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolutionWidth",
                table: "Sighting",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SharpnessScore",
                table: "Sighting",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlaggedForReview",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "LuminanceAverage",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "QualityTier",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "ResolutionHeight",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "ResolutionWidth",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "SharpnessScore",
                table: "Sighting");
        }
    }
}
