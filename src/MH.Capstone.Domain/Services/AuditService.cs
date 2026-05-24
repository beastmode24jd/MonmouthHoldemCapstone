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
using System.Linq;

namespace MH.Capstone.Domain.Services
{
    public class AuditService : IAuditService
    {
        private readonly IRepository<Report, ApplicationDbContext> _reportRepo;
        public readonly IRepository<AuditLog, ApplicationDbContext> _auditRepo;

        public AuditService(
            IRepository<Report, ApplicationDbContext> reportRepo,
            IRepository<AuditLog, ApplicationDbContext> auditRepo)
        {
            _reportRepo = reportRepo;
            _auditRepo = auditRepo;
        }

        // Create an audit for the submitted report argument.
        // Return false if unsuccessful.
        public async Task<bool> LogActionAsync(AuditLog audit)
        {
            if (audit == null) return false;
            var result = await _auditRepo.AddOrUpdateAsync(audit);
            return result != null;
        }

        // Get all the Audits for page display.
        // Base method for getting a simple paged list (Default view)
        public async Task<(List<AuditLog> Audits, int TotalCount)> GetPagedAuditsAsync(int page, int pageSize)
        {
            var query = await _auditRepo.GetAllAsync(); 
            return await ExecuteAuditQueryAsync(query, page, pageSize);
        }

        // Filter function, allows audits to be searched by date.
        // Return false if unsuccessful.
        public async Task<(List<AuditLog> Audits, int TotalCount)> GetAuditsByDateRangeAsync(
            DateTimeOffset start, DateTimeOffset end, int page, int pageSize)
        {
            // Await the base query
            var baseQuery = await _auditRepo.GetAllAsync();
            
            // Can apply LINQ directly because baseQuery is an IQueryable
            var query = baseQuery.Where(a => a.Timestamp >= start && a.Timestamp <= end);
            
            return await ExecuteAuditQueryAsync(query, page, pageSize);
        }

        public async Task<(List<AuditLog> Audits, int TotalCount)> GetAuditsByAdminAsync(
            Guid adminId, int page, int pageSize)
        {
            // Await the base query
            var baseQuery = await _auditRepo.GetAllAsync();
            
            var query = baseQuery.Where(a => a.PerformingUserId == adminId);
            
            return await ExecuteAuditQueryAsync(query, page, pageSize);
        }

        public async Task<(List<AuditLog> Audits, int TotalCount)> GetAuditsByUserAsync(
            Guid userId, int page, int pageSize)
        {
            // Await the base query
            var baseQuery = await _auditRepo.GetAllAsync();
            
            var query = baseQuery.Where(a => a.TargetUserId == userId);
            
            return await ExecuteAuditQueryAsync(query, page, pageSize);
        }

        public async Task<(List<AuditLog> Audits, int TotalCount)> GetAuditsByActionAsync(
            AuditActionType action, int page, int pageSize)
        {
            // Await the base query
            var baseQuery = await _auditRepo.GetAllAsync();
            
            var query = baseQuery.Where(a => a.ActionType == action);
            
            return await ExecuteAuditQueryAsync(query, page, pageSize);
        }

        // Private helper
        // Handles shared logic for Pagination and Eager Loading (Include)
        private async Task<(List<AuditLog> Audits, int TotalCount)> ExecuteAuditQueryAsync(IQueryable<AuditLog> query, int page, int pageSize)
        {
            // CountAsync and ToListAsync run the SQL efficiently on the database
            int totalCount = await query.CountAsync();
            
            var audits = await query
                .Include(a => a.PerformingUser)
                .Include(a => a.TargetUser)
                .Include(a => a.TargetReport)
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (audits, totalCount);
        }
    }
}