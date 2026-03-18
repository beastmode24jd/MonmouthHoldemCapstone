using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IReportService
    {
        // Submit a report. Will return false if the user has already reported this page URL.
        Task<bool> SubmitReportAsync(Report report);
    }
}