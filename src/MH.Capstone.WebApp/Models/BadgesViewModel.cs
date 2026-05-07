namespace MH.Capstone.WebApp.Models
{
    public class BadgesViewModel
    {
        public List<MH.Capstone.Domain.DataModels.Badge> AllBadges { get; set; } = new();

        // The badges earned by the current user.
        /// These badges contain the local-timezone converted 'BadgeEarned' timestamps.
        public List<Domain.DataModels.UserBadge> UserBadges { get; set; } = new();
        public Guid CurrentUserId { get; set; }
    }
}