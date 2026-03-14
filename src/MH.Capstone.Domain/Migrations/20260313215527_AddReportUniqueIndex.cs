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
                name: "IX_Report_ReportingUserIdentityId_ReportedPageUrl_IsResolved",
                table: "Report",
                columns: new[] { "ReportingUserId", "ReportedPageUrl", "IsResolved" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Report_ReportingUserIdentityId_ReportedPageUrl_IsResolved",
                table: "Report");
        }
    }
}
