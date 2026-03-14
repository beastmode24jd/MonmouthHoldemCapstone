using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MH.Capstone.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddReportUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Report_ReportingUserIdentityId_ReportedPageUrl",
                table: "Report",
                columns: new[] { "ReportingUserId", "ReportedPageUrl" },
                unique: true,
                filter: "[IsResolved] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Report_ReportingUserIdentityId_ReportedPageUrl",
                table: "Report");
        }
    }
}
