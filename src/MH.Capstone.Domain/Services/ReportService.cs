using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Identity;

namespace MH.Capstone.Domain.Services
{
    public class ReportService : IReportService
    {
        private readonly IRepository<Report, ApplicationDbContext> _reportRepo;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _userRepo;

        public ReportService(
            IRepository<Report, ApplicationDbContext> reportRepo,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            IRepository<ApplicationUser, ApplicationDbContext> userRepo)
        {
            _reportRepo = reportRepo;
            _notificationService = notificationService;
            _userManager = userManager;
            _userRepo = userRepo;
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

        public async Task<List<Report>> SortReports(string adminId,
            ReportFilterType reportType,
            string? pageURL,
            string? reportingUserId,
            DateTime? date)
        {
            // ReportFilterType values:
            //      pageURL == 0
            //      reportingUserId == reporter
            //      date == 2

            //      Respective argument fields are nullable to be omitted as needed.

            // Sort and check adminId with _userManager ***********
            var user = await _userManager.FindByIdAsync(adminId);

            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                throw new UnauthorizedAccessException("Access Denied: You do not have permission to view or sort reports.");
            }
            
            // Get all the reports available
            IQueryable<Report> query = await _reportRepo.GetAllAsync();

            // Apply ReportFilterType (Only if arguments are provided)
            if (!string.IsNullOrWhiteSpace(pageURL))
            {
                query = query.Where(r => r.ReportedPageUrl.Contains(pageURL));
            }

            if (!string.IsNullOrWhiteSpace(reportingUserId))
            {
                query = query.Where(r => r.ReportingUserIdentityId == reportingUserId);
            }

            if (date.HasValue)
            {
                // Filters for reports submitted on or after the provided date
                query = query.Where(r => r.SubmittedAt >= date.Value);
            }

            // Apply Sorting
            query = reportType switch
            {
                ReportFilterType.PageURL => query.OrderBy(r => r.ReportedPageUrl),
                ReportFilterType.Reporter => query.OrderBy(r => r.ReportingUserIdentityId),
                ReportFilterType.Date => query.OrderByDescending(r => r.SubmittedAt),
                _ => query.OrderByDescending(r => r.SubmittedAt) // Default to newest at the top of the display
            };
            
            return await query.ToListAsync();
        }
    }
}
