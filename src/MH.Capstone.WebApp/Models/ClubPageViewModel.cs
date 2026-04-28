using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class ClubPageViewModel
    {
        public Club Club { get; set; } = null!;
        public List<ClubMembership> ClubMembers { get; set; } = new();
        public bool IsCurrentUserOwner { get; set; }
        public bool IsCurrentUserMember { get; set; }

        public ClubPageViewModel() { }

        public ClubPageViewModel(Club club, List<ClubMembership> clubMembers, bool isOwner, bool isMember)
        {
            Club = club;
            ClubMembers = clubMembers;
            IsCurrentUserOwner = isOwner;
            IsCurrentUserMember = isMember;
        }
    }
}
