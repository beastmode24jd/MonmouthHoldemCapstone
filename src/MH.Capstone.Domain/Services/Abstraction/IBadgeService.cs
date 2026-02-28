using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IBadgeService
    {
        Task AddBadge(ApplicationUser user, int badgeID);
        Task GetBadgeDetails(int badgeID);
    }

}