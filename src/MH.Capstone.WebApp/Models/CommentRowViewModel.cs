namespace MH.Capstone.WebApp.Models
{
    // CSP-187: one row in the comments list under a sighting's details page.
    public class CommentRowViewModel
    {
        public Guid Id { get; set; }

        public Guid AuthorId { get; set; }

        public string AuthorDisplayName { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
