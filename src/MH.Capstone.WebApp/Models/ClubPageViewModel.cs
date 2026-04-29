using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class ClubPageViewModel
    {
        public Club Club { get; set; } = null!;
        public List<ClubMembership> ClubMembers { get; set; } = new();
        public List<SightingCardViewModel> Sightings { get; set; } = new();
        public bool HasSightings => Sightings.Count > 0;
        public bool IsCurrentUserOwner { get; set; }
        public bool IsCurrentUserMember { get; set; }

        public List<ClubMembership> TopMembersByPoints { get; set; } = new();
        public ClubMembership? NewestMember { get; set; }
        public int TotalSightingsCount { get; set; }

        public ClubPageViewModel() { }

        public ClubPageViewModel(Club club, List<ClubMembership> clubMembers,
            IEnumerable<Sighting> sightings, int totalSightingsCount, bool isOwner, bool isMember)
        {
            Club = club;
            ClubMembers = clubMembers;
            Sightings = sightings.Select(s => new SightingCardViewModel(s)).ToList();
            TotalSightingsCount = totalSightingsCount;
            IsCurrentUserOwner = isOwner;
            IsCurrentUserMember = isMember;

            TopMembersByPoints = clubMembers
                .OrderByDescending(m => m.Member?.Points ?? 0)
                .Take(3)
                .ToList();

            NewestMember = clubMembers
                .OrderByDescending(m => m.JoinedAt)
                .FirstOrDefault();
        }
    }
}
