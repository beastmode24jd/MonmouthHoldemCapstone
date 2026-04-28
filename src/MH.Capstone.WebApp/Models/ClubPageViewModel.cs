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

        public ClubPageViewModel() { }

        public ClubPageViewModel(Club club, List<ClubMembership> clubMembers,
            List<SightingCardViewModel> sightings, bool isOwner, bool isMember)
        {
            Club = club;
            ClubMembers = clubMembers;
            Sightings = sightings;
            IsCurrentUserOwner = isOwner;
            IsCurrentUserMember = isMember;
        }
    }
}
