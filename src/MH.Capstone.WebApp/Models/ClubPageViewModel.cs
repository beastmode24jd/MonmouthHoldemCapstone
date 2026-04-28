using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class ClubPageViewModel
    {
        public Club Club { get; set; } = null!;
        public List<ClubMembership> ClubMembers { get; set; } = new();
        public List<Sighting> Sightings { get; set; } = new();
        public bool HasSightings { get; set; } = false;
        public bool IsCurrentUserOwner { get; set; }
        public bool IsCurrentUserMember { get; set; }

        public ClubPageViewModel() { }

        public ClubPageViewModel(Club club, List<ClubMembership> clubMembers, 
        List<Sighting> sightings, bool hasSightings,
        bool isOwner, bool isMember)
        {
            Club = club;
            ClubMembers = clubMembers;
            Sightings = sightings;
            HasSightings = hasSightings;
            IsCurrentUserOwner = isOwner;
            IsCurrentUserMember = isMember;
        }
    }
}
