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

        public async Task<(List<Report> Reports, int TotalCount)> SortReports(
            ReportFilterType reportType,
            string? pageURL,
            string? reportingUserId,
            DateTime? date,
            bool showResolved, // false means this isn't selected
            int page,          
            int pageSize)
        {
            // ReportFilterType values:
            //      pageURL == 0
            //      reportingUserId == reporter
            //      date == 2
            //      resolved == 3

            //      Respective argument fields are nullable to be omitted as needed.

            // AdminId check should go in Controller, before the ReportService method call
            
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
                query = query.Where(r => r.SubmittedAt <= date.Value);
            }
            
            if (showResolved == true)
            {
                query = query.Where(r => r.IsResolved == true);
            }

            // Get total query list count before pagination
            int totalCount = await query.CountAsync();

            // Apply Sorting
            query = reportType switch
            {
                ReportFilterType.PageURL => query.OrderBy(r => r.ReportedPageUrl),
                ReportFilterType.Reporter => query.OrderBy(r => r.ReportingUserIdentityId),
                ReportFilterType.Date => query.OrderByDescending(r => r.SubmittedAt),
                ReportFilterType.Resolved => query.OrderByDescending(r => r.SubmittedAt),
                _ => query.OrderByDescending(r => r.SubmittedAt) // Default to newest at the top of the display
            };

            // Apply Pagination
            var reports = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reports, totalCount);
        }
    }
}
