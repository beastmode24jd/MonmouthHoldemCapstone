using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class FixSightingUserIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                    name: "UserId",
                    table: "Sighting",
                    type: "nvarchar(450)",
                    maxLength: 450,
                    nullable: false,
                    oldClrType: typeof(Guid),
                    oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                    name: "UserId",
                    table: "Sighting",
                    type: "uniqueidentifier",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "nvarchar(450)",
                    oldMaxLength: 450);
        }
    }
}
