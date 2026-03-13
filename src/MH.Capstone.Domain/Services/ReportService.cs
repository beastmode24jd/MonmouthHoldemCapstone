using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;

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

            // NEW - O(log n) single indexed DB lookup instead of O(n) in memory scan
            var existing = await _reportRepo.GetAllAsync(r =>
                r.ReportingUserIdentityId == report.ReportingUserIdentityId &&
                r.ReportedPageUrl == report.ReportedPageUrl &&
                !r.IsResolved);
            bool isDuplicate = existing.Any();

            if (isDuplicate)
            {
                return false;
            }

            // Save the report
            await _reportRepo.AddOrUpdateAsync(report);

            // Send confirmation notification to the reporting user
            await _notificationService.SendNotificationAsync(Notification.Create(
                report.ReportingUserId,
                "Report Received",
                $"Thank you. Your report for '{report.ReportedPageUrl}' has been received and is under review."
            ));

            return true;
        }
    }
}
