namespace MH.Capstone.WebApp.Models
{
    // CSP-187: activity feed showing sightings from users you follow, newest first.
    public class FeedViewModel
    {
        public List<SightingCardViewModel> Sightings { get; set; } = new();

        public bool HasFollowees { get; set; }
    }
}
