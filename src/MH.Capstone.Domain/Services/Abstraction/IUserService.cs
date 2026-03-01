using MH.Capstone.Domain.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Services.Abstraction
{
    public interface IUserService : IUserProfileService
    {
        Task<bool> UserExistsAsync(string identifier);

        Task<ApplicationUser?> GetUserByEmailAsync(string email);

        Task<ApplicationUser?> GetUserByIdAsync(string id);

        Task<ApplicationUser?> GetUserByIdAsync(Guid id) => GetUserByIdAsync(id.ToString());
    }

    public interface IUserProfileService
    {
        Task UpdateUserBioAsync(ApplicationUser user, string? newBio);

        Task UpdateUserProfileImageAsync(string email, byte[] pictureData, string contentType);
    }
}
