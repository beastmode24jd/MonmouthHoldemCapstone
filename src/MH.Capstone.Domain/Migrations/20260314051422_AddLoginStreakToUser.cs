using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginStreakToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLogin",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoginStreak",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LoginStreak",
                table: "AspNetUsers");
        }
    }
}
