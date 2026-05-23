using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IAuditService
    {
        // Create an audit for the submitted report argument.
        // Return false if unsuccessful.
        Task<bool> LogAuditAsync(Report report);

        // Get all the Audits for page display.
        // Sort newest first. Use pagination?
        Task<(List<AuditLog> Audits, int TotalCount)> ListAuditAsync();

        // Filter function, allows audits to be searched by date.
        // Return false if unsuccessful.
        Task<bool> SearchAuditsByDate(DateTimeOffset date);

        Task<bool> SearchAuditsByAdmin(Guid adminId);

        Task<bool> SearchAuditsByPage(string pageUrl);
    }
}