using System.ComponentModel.DataAnnotations;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.WebApp.Models
{
    public class AccountViewModel
    {
        public Guid Id { get; set; }

        public string? Username { get; set; }

        public string? DisplayName { get; set; }
        public int? Points { get; set; }

        public bool IsDeactivated { get; set; } = false; // Default to false since the deactivation status is unknown.

        public string ProfileImageUrl { get; set; } = string.Empty; // Initialize to avoid null reference issues.

        public bool IsAuthenticatedUser { get; set; } = false; // Default to false since the current authenticated user is unknown.

        /* Default to placeholder prompt for user to submit custom bio.
           Accepts nulls and reverts them to placeholder string, in case
           this is called on older account entries without a bio parameter. */
        public string? Bio { get; set; } = "Enter a unique profile bio.";

        // CSP-187: follow/block state relative to the currently authenticated viewer.
        // Both default to false; controller only populates them when viewing another user.
        public bool IsFollowedByCurrentUser { get; set; } = false;

        public bool IsBlockedByCurrentUser { get; set; } = false;

        // CSP-211: total follower/following counts surfaced on the profile header.
        public int FollowerCount { get; set; } = 0;
        public int FolloweeCount { get; set; } = 0;

        // CSP-211: paginated rows rendered under the follower/following tabs.
        // Controller fills with the requested page (size 20).
        public IList<FollowListUser> Followers { get; set; } = new List<FollowListUser>();
        public IList<FollowListUser> Followees { get; set; } = new List<FollowListUser>();
        public int FollowersPage { get; set; } = 1;
        public int FolloweesPage { get; set; } = 1;
        public const int FollowPageSize = 20;

        public int FollowersTotalPages => Math.Max(1, (int)Math.Ceiling((double)FollowerCount / FollowPageSize));
        public int FolloweesTotalPages => Math.Max(1, (int)Math.Ceiling((double)FolloweeCount / FollowPageSize));

        // Sprint 7: Top 3 most recently earned badge titles, descending. Empty when the
        // user has earned none. Controller populates; the ApplicationUser ctor leaves it empty.
        public IReadOnlyList<string> RecentBadgeTitles { get; set; } = Array.Empty<string>();

        // Sprint 7: Top 3 clubs the user has accepted membership in. Empty when the user is
        // in no clubs. Controller populates; the ApplicationUser ctor leaves it empty.
        public IReadOnlyList<ProfileClubLink> RecentClubs { get; set; } = Array.Empty<ProfileClubLink>();

        // Sprint 7: Top 3 most recent sightings the user has submitted, newest-first by Timestamp.
        // Reuses SightingCardViewModel so cards on the profile mirror the Gallery styling.
        public IReadOnlyList<SightingCardViewModel> RecentSightings { get; set; } = Array.Empty<SightingCardViewModel>();

        // For ASP.NET Core model binding
        public AccountViewModel() { }

        // Constructor to initialize the view model from an ApplicationUser
        public AccountViewModel(ApplicationUser user, bool isAuthedUser = false)
        {
            Id = Guid.Parse(user.Id);
            Username = user.UserName;
            DisplayName = user.DisplayName;
            ProfileImageUrl = user.GetProfileImageUrl();
            IsAuthenticatedUser = isAuthedUser;
            IsDeactivated = user.IsDeactivated;
            Bio = user.Bio;
            Points = user.Points;
        }
    }

    // CSP-211: per-row payload for the follower/following list tabs.
    public class FollowListUser
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
    }

    // Sprint 7: Minimal projection used by the profile "Recent Clubs" list. Carries only the
    // fields the view needs (id for the link target, name + description for the button label)
    // so the domain Club entity stays out of the view layer. Description is nullable to mirror
    // Club.Description.
    public record ProfileClubLink(Guid Id, string Name, string? Description);
}
