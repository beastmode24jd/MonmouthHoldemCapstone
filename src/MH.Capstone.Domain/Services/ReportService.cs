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

        public async Task<(List<Report> Reports, int TotalCount)> SortReports(
            ReportFilterType reportType,
            string? pageURL,
            string? reportingUserId,
            DateTimeOffset? date, // Modify with userZone to get dates for front-end display?
            bool? showResolved, // null means this isn't selected
            int page,          
            int pageSize,
            TimeZoneInfo userZone)
        {
            // ReportFilterType values:
            //      pageURL == 0
            //      reportingUserId == reporter
            //      date == 2
            //      resolved == 3

            //      Respective argument fields nullable to be omitted as needed.
            
            // Get all the reports available
            IQueryable<Report> query = (await _reportRepo.GetAllAsync()).Include(r => r.Reporter);

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
                // Filters for reports submitted on or after the provided date (in UTC)
                query = query.Where(r => r.SubmittedAt <= date.Value);
            }
            
            if (showResolved.HasValue)
            {
                query = query.Where(r => r.IsResolved == showResolved.Value);
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

            // DateTimeOffset Conversion:
            //      Adjust the offset for each report for local display
            foreach (var report in reports)
            {
                // Changes the offset of the object to match the user's timezone
                report.SubmittedAt = TimeZoneInfo.ConvertTime(report.SubmittedAt, userZone);
            }

            return (reports, totalCount);
        }
        public async Task<bool> SetReportResolution(Guid reportId, bool isResolved)
        {
            // Get the report
            var report = await _reportRepo.FindByIdAsync(reportId);

            if (report == null)
            {
                // Report not found, return false
                return false;
            }

            // Connect IsResolved to the bool input
            report.IsResolved = isResolved;

            // Notification Update to the user who submitted the report
            if (report.IsResolved)
            {
                await _notificationService.SendNotificationAsync(Notification.Create(
                    report.ReportingUserId,
                    "Report Resolved",
                    $"Your report for '{report.ReportedPageUrl}' has been closed."
                ), NotificationType.ReportStatusUpdate);
            }
            else if (!report.IsResolved)
            {
                // Report was re-opened by Admin, notify user
                await _notificationService.SendNotificationAsync(Notification.Create(
                    report.ReportingUserId,
                    "Report Re-Opened",
                    $"Your report for '{report.ReportedPageUrl}' has been re-opened."
                ), NotificationType.ReportStatusUpdate);
            }

            await _reportRepo.AddOrUpdateAsync(report);
            return true;
        }

        public async Task<Report?> GetReportByIdAsync(Guid reportId)
        {
            var report = await _reportRepo.FindByIdAsync(reportId);
            return report;
        }
    }
}
