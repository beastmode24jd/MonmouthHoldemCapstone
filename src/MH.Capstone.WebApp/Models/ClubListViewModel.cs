using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class ClubListViewModel
    {
        public List<Club> PublicClubs { get; set; } = new();
        public List<Club> UserClubs { get; set; } = new();
        public List<Club> PendingInvites { get; set; } = new();
        public string CurrentUserId { get; set; } = string.Empty;

        public bool HasPublicClubs => PublicClubs.Any();
        public bool HasPersonalClubs => UserClubs.Any();
        public bool HasPendingInvites => PendingInvites.Any();
        public int PublicClubCount => PublicClubs.Count;
        public int UserClubCount => UserClubs.Count;

        public ClubListViewModel() { }

        public ClubListViewModel(
            IEnumerable<Club> publicClubs,
            IEnumerable<Club> userClubs,
            string currentUserId = "",
            IEnumerable<Club>? pendingInvites = null)
        {
            PublicClubs = publicClubs.ToList();
            UserClubs = userClubs.ToList();
            CurrentUserId = currentUserId;
            PendingInvites = pendingInvites?.ToList() ?? new List<Club>();
        }
    }
}
