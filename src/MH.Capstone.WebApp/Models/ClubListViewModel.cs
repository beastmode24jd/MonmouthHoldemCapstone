using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    // Page-level ViewModel for the Clubs landing page.
    // Public clubs are visible to all authenticated users.
    // Private clubs list only includes clubs the current user is a member of.
    // The controller is responsible for fetching both lists via IClubService and passing them here.
    public class ClubListViewModel
    {
        public List<Club> PublicClubs { get; set; } = new();
        public List<Club> UserClubs { get; set; } = new();
        public string CurrentUserId { get; set; } = string.Empty;

        public bool HasPublicClubs => PublicClubs.Any();
        public bool HasPersonalClubs => UserClubs.Any();
        public int PublicClubCount => PublicClubs.Count;
        public int UserClubCount => UserClubs.Count;

        public ClubListViewModel() { }

        public ClubListViewModel(IEnumerable<Club> publicClubs, IEnumerable<Club> userClubs, string currentUserId = "")
        {
            PublicClubs = publicClubs.ToList();
            UserClubs = userClubs.ToList();
            CurrentUserId = currentUserId;
        }
    }
}
