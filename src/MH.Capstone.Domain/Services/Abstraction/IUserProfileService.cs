using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataModels;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IUserProfileService
    {
        void UpdateUserBio(ApplicationUser user, string? newBio);
    }

}