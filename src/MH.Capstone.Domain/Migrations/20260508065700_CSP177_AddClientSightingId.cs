using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class CSP177_AddClientSightingId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sighting_UserId",
                table: "Sighting");

            migrationBuilder.AddColumn<string>(
                name: "ClientSightingId",
                table: "Sighting",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sighting_UserId_ClientSightingId",
                table: "Sighting",
                columns: new[] { "UserId", "ClientSightingId" },
                unique: true,
                filter: "[ClientSightingId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sighting_UserId_ClientSightingId",
                table: "Sighting");

            migrationBuilder.DropColumn(
                name: "ClientSightingId",
                table: "Sighting");

            migrationBuilder.CreateIndex(
                name: "IX_Sighting_UserId",
                table: "Sighting",
                column: "UserId");
        }
    }
}
