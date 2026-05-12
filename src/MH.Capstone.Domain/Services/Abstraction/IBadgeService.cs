using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IBadgeService
    {
        // Awards a Badge, run after doing checks
        Task AddBadge(ApplicationUser user, Guid badgeID, string ianaTimeZoneId = "America/Los_Angeles");

        // Updates BadgeProgress, runs AddBadge if it hits BadgeStep amount
        Task UpdateBadge(ApplicationUser user, Guid badgeID, string ianaTimeZoneId = "America/Los_Angeles");

        // Updates older accounts with new Badges, makes sure BadgeProgress matches their accounts
        Task SyncBadgeProgressAsync(ApplicationUser user, Guid badgeID, int actualCount, string ianaTimeZoneId = "America/Los_Angeles");

        Task<Badge?> GetBadgeDetails(Guid badgeID);
        Task<List<UserBadge>> SortBadgesByTime(List<UserBadge> badgeList);

        // Badge initialization is handled in ApplicationDbContextSeeding.cs
    }

}