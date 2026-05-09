using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IBadgeService
    {
        Task AddBadge(ApplicationUser user, Guid badgeID, string ianaTimeZoneId = "America/Los_Angeles");
        Task UpdateBadge(ApplicationUser user, Guid badgeID, string ianaTimeZoneId = "America/Los_Angeles");
        Task<Badge?> GetBadgeDetails(Guid badgeID);

        Task<List<UserBadge>> SortBadgesByTime(List<UserBadge> badgeList);

        // Badge initialization is handled in ApplicationDbContextSeeding.cs
    }

}