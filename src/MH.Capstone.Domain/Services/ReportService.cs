using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace MH.Capstone.Domain.Services
{
    public class ReportService : IReportService
    {
        private readonly IRepository<Report, ApplicationDbContext> _reportRepo;
        private readonly INotificationService _notificationService;

        public ReportService(
            IRepository<Report, ApplicationDbContext> reportRepo,
            INotificationService notificationService)
        {
            _reportRepo = reportRepo;
            _notificationService = notificationService;
        }

        public async Task<bool> SubmitReportAsync(Report report)
        {
            if (!report.TryValidateEntity(out var fails))
            {
                var firstFail = fails.First();
                throw new ArgumentException($"Report entity validation failed. Property {firstFail} invalid.",
                    firstFail);
            }

            try
            {
                // try to save the report, and let the database handle the duplicate report check
                await _reportRepo.AddOrUpdateAsync(report);

                // notification to the reporting user
                await _notificationService.SendNotificationAsync(Notification.Create(
                    report.ReportingUserId,
                    "Report Received",
                    $"Thank you. Your report for '{report.ReportedPageUrl}' has been received and is under review."
                ), NotificationType.ReportStatusUpdate);

                return true;
            }
            catch (Exception ex)
            {
                // Check if this is a unique constraint violation (duplicate unresolved report)
                if ((ex is SqlException sqlEx && sqlEx.IsOfErrorType(SqlErrorNumber.UniqueConstraintViolation)) ||
                    (ex is DbUpdateException dbEx && dbEx.IsOfErrorType(SqlErrorNumber.UniqueConstraintViolation)))
                {
                    // if dupe report dectected
                    return false;
                }

                throw;
            }
        }

        // Need methods to filter data based on page URL,
        //      reporter (associated ApplicationUser),
        //      and date.

        // Pass in different argument for different sorting systems.
        //      Reuse the general code.
        
        // NOTE: Make a data model enum for sorting different types later.
        //      Will combine with Report field update to DateTime,
        //      for bundled EF migration.

        // Sort by Descending.

        public async Task<List<Report>> SortReports(string adminId, int type,
            string? pageURL,
            string? reportingUserId,
            DateTime? date)
        {
            // Int types:
            //      0 == page sort
            //      1 == reporter sort
            //      2 == date sort
            //      Parameter fields are nullable to be omitted as needed.


            
            // Placeholder return value
            return new List<Report>();
        }
    }
}
