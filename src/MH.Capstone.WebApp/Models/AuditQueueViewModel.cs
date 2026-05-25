using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MH.Capstone.WebApp.Models
{
    public class AuditQueueViewModel
    {
        // Data
        public List<AuditLog> Audits { get; set; } = new();

        // Filter values
        public DateTimeOffset? DateFilter { get; set; }
        public string? AdminSearch { get; set; }
        public string? UserSearch { get; set; }
        public string? SortBy { get; set; }

        // Pagination data
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }
}