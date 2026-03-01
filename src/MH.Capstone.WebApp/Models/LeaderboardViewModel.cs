using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class LeaderboardViewModel
    {
        
        public List<ApplicationUser> Users { get; set; } = new();   
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? CurrentUserId { get; set; }
        public int UserRank { get; set; }
        public int UserPage => UserRank == 0 ? 1 : (int)Math.Ceiling((double)UserRank / 30);
    }
}