using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IReportService
    {
        // Submit a report. Will return false if the user has already reported this page URL.
        Task<bool> SubmitReportAsync(Report report);

        // Sort reports. Checks if adminId is valid, then returns sorted report list
        //      List will depend on passed in value
        //          List will return empty if there are no reports.
        Task<(List<Report> Reports, int TotalCount)> SortReports(
            ReportFilterType reportType,
            string? pageURL,
            string? reportingUserId,
            DateTimeOffset? date,
            bool? showResolved, // null means this isn't selected
            int page,          
            int pageSize,
            TimeZoneInfo userZone);

        // Reverse the bool value of a Report (Opens if Resolved, Resolves if Open)
        // Returns false if the report is not found.
        Task<bool> SetReportResolution(Guid reportId, bool isResolved);
    }
}