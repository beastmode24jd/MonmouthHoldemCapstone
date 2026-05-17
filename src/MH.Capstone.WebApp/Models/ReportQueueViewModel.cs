using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MH.Capstone.WebApp.Models
{
    public class ReportQueueViewModel
    {
        // Data
        public List<Report> Reports { get; set; } = new();

        // This will hold the pre-processed ReportFilterType options list for the View
        public List<SelectListItem> SortOptions { get; set; } = new();

        // Filtering & Sorting
        public string? PageUrlFilter { get; set; }
        public string? ReporterIdFilter { get; set; }
        public string? UserSearch { get; set; }
        public DateTimeOffset? DateFilter { get; set; }
        public ReportFilterType SortBy { get; set; }
        public bool? ShowResolved { get; set; }

        // Pagination data
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }
}