using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReportWithDateTimeOffset_AddUserAccountLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "Report",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "accountLocked",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accountLocked",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "Report",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");
        }
    }
}
