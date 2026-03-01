using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    BadgeID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PointValue = table.Column<int>(type: "int", nullable: false),
                    BadgeIcon = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.BadgeID);
                });

            migrationBuilder.CreateTable(
                name: "PersonalBadges",
                columns: table => new
                {
                    UserBadgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<string>(name: "User ID", type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BadgeID = table.Column<Guid>(name: "Badge ID", type: "uniqueidentifier", nullable: false),
                    BadgeEarned = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalBadges", x => x.UserBadgeId);
                    table.ForeignKey(
                        name: "FK_PersonalBadges_AspNetUsers_User ID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonalBadges_Badges_Badge ID",
                        column: x => x.BadgeID,
                        principalTable: "Badges",
                        principalColumn: "BadgeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalBadges_Badge ID",
                table: "PersonalBadges",
                column: "Badge ID");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalBadges_User ID",
                table: "PersonalBadges",
                column: "User ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalBadges");

            migrationBuilder.DropTable(
                name: "Badges");
        }
    }
}
