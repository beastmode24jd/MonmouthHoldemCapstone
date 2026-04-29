using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class ClubPageViewModel
    {
        public Club Club { get; set; } = null!;
        public bool IsCurrentUserOwner { get; set; }
        public bool IsCurrentUserMember { get; set; }

        public ClubPageViewModel() { }

        public ClubPageViewModel(Club club, bool isOwner, bool isMember)
        {
            Club = club;
            IsCurrentUserOwner = isOwner;
            IsCurrentUserMember = isMember;
        }
    }
}
