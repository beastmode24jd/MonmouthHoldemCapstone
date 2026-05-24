using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IAuditService
    {
        // Create an audit for the submitted report argument.
        // Return false if unsuccessful.
        Task<bool> LogActionAsync(AuditLog audit);

        // Get all the Audits for page display.
        // Sort newest first.
        Task<(List<AuditLog> Audits, int TotalCount)> GetPagedAuditsAsync(int page, int pageSize);

        // Filter function, allows audits to be searched by date.
        // Return false if unsuccessful.
        Task<(List<AuditLog> Audits, int TotalCount)> GetAuditsByDateRangeAsync(
            DateTimeOffset start, DateTimeOffset end, int page, int pageSize);

        Task<(List<AuditLog> Audits, int TotalCount)> GetAuditsByAdminAsync(
            Guid adminId, int page, int pageSize);

        Task<(List<AuditLog> Audits, int TotalCount)> GetAuditsByActionAsync(
            AuditActionType action, int page, int pageSize);
    }
}