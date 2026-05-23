using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MH.Capstone.WebApp.Models
{
    public class AuditQueueViewModel
    {
        // Data
        public List<Report> Audits { get; set; } = new();
    }
}