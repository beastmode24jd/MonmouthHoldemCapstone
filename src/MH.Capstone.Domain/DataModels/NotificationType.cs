using System.ComponentModel.DataAnnotations;

namespace MH.Capstone.Domain.DataModels
{
    /// <summary>
    /// Values without a [Display] attribute (SystemCritical) are non-configurable by users
    /// and are excluded automatically from the notification preferences UI.
    /// </summary>
    public enum NotificationType
    {
        SystemCritical = 0,

        [Display(Name = "Badge Awarded",
                 Description = "Sent when you earn a new achievement badge.")]
        BadgeAwarded = 1,

        [Display(Name = "Report Status Update",
                 Description = "Sent when a content report you submitted is received or updated.")]
        ReportStatusUpdate = 2,

        [Display(Name = "New Sighting Activity",
                 Description = "Sent when you successfully upload a new wildlife sighting.")]
        NewSightingActivity = 3,

        [Display(Name = "Account Activity",
                 Description = "Sent when your account records a login or active streak.")]
        AccountActivity = 4,

        [Display(Name = "Club Activity",
                 Description = "Sent when there is new activity in your clubs, such as invitations or member sightings.")]
        ClubActivity = 5,
    }
}
