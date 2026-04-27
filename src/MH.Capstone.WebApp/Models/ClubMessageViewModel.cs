using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class ClubMessageViewModel
    {
        public Club Club { get; set; } = null!;
        public List<Message> Messages { get; set; } = new();
        public string CurrentUserId { get; set; } = string.Empty;
        public bool IsCurrentUserMember { get; set; }

        public bool HasMessages => Messages.Any();

        public ClubMessageViewModel() { }

        public ClubMessageViewModel(Club club, List<Message> messages, string currentUserId, bool isCurrentUserMember)
        {
            Club = club;
            Messages = messages;
            CurrentUserId = currentUserId;
            IsCurrentUserMember = isCurrentUserMember;
        }
    }
}
