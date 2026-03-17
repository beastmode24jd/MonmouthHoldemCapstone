#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace MH.Capstone.Domain.Migrations.Cache
{
    /// <inheritdoc />
    public partial class AddCacheDbContextAndAnimalApiCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalApiCache",
                columns: table => new
                {
                    Url = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    QueryParams = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CachedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CachedResponse = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalApiCache", x => new { x.Url, x.QueryParams });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalApiCache");
        }
    }
}
