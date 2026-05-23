using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.Domain.Services
{
    public class AuditService : IAuditService
    {
        private readonly IRepository<Report, ApplicationDbContext> _reportRepo;

        public AuditService(
            IRepository<Report, ApplicationDbContext> reportRepo,
            INotificationService notificationService)
        {
            _reportRepo = reportRepo;
        }

        // Create an audit for the submitted report argument.
        // Return false if unsuccessful.
        public async Task<bool> LogAuditAsync(Report report)
        {
            return false;
        }

        // Get all the Audits for page display.
        // Sort newest first. Use pagination?
        public async Task<(List<AuditLog> Audits, int TotalCount)> ListAuditAsync()
        {
            int totalCount = 0;
            return (new List<AuditLog>(), totalCount);
        }

        // Filter function, allows audits to be searched by date.
        // Return false if unsuccessful.
        public async Task<bool> SearchAuditsByDate(DateTimeOffset date)
        {
            return false;
        }

        public async Task<bool> SearchAuditsByAdmin(Guid adminId)
        {
            return false;
        }

        public async Task<bool> SearchAuditsByPage(string pageUrl)
        {
            return false;
        }
    }
}