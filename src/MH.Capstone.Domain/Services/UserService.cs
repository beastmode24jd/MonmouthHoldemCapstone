using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;

namespace MH.Capstone.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _userRepo;

        public UserService(UserManager<ApplicationUser> userManager, 
            IRepository<ApplicationUser, ApplicationDbContext> userRepo, ILogger<UserService> logger)
        {
            _userManager = userManager;
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<bool> UserExistsAsync(string email) =>
            await _userManager.FindByEmailAsync(email) != null;

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email) =>
            await _userManager.FindByEmailAsync(email);

        public async Task<ApplicationUser?> GetUserByIdAsync(string id) =>
            await _userManager.FindByIdAsync(id);

        public async Task UpdateUserBioAsync(ApplicationUser user, string? newBio)
        {
            // Doesn't reset the Bio string's default, if it is over 250 char.
            if (string.IsNullOrWhiteSpace(newBio) || newBio.Length > 250)
            {
                // Invalid bio, throw an exception.
                throw new ArgumentOutOfRangeException(nameof(newBio), "Setting a bio requires a non-null string between 1-250 characters long");
            }

            // Update & Save Changes
            user.Bio = newBio;
            await user.SaveModelAsync(_userRepo);
        }

        public async Task UpdateUserProfileImageAsync(string email, byte[] pictureData, string contentType)
        {
            var user = await GetUserByEmailAsync(email);
            if (user != null)
            {
                user.ProfileImage = pictureData;
                user.ProfileImageType = contentType;
                await user.SaveModelAsync(_userRepo);
                _logger.LogInformation("Updated profile image for {Email}.", email);
            }
            else
            {
                _logger.LogInformation("User with {Email} email not found.", email);
            }
        }
    }
}
