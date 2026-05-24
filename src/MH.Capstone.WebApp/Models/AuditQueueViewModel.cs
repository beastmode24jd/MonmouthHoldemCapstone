using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MH.Capstone.WebApp.Models
{
    public class AuditQueueViewModel
    {
        // Data
        public List<AuditLog> Audits { get; set; } = new();

        // Pagination data
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }
}