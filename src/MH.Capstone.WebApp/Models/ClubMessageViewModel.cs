using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class ClubMessageViewModel
    {
        // For title and ID data
        public Club Club { get; set; } = null!;

        // User GUID for message init
        public string CurrentUserId { get; set; } = string.Empty;

        public List<Message> Messages { get; set; } = new List<Message>();
        public ClubMessageViewModel() { }
        public ClubMessageViewModel(Club club, List<Message> messages, string currentUserId = "")
        {
            Club = club;
            Messages = messages;
            CurrentUserId = currentUserId;
        }
    }
}