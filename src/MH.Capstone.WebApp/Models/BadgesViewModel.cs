namespace MH.Capstone.WebApp.Models
{
    public class BadgesViewModel
    {
        public List<MH.Capstone.Domain.DataModels.Badge> AllBadges { get; set; } = new();
        public List<Guid> EarnedBadgeIds { get; set; } = new();
        public Guid CurrentUserId { get; set; }
    }
}