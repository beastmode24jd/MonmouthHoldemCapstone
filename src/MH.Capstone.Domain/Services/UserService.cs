using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        public async Task<ApplicationUser?> GetUserByClaimsPrincipleAsync(ClaimsPrincipal user)
            => await _userManager.GetUserAsync(user);

        public async Task UpdateUserBioAsync(ApplicationUser user, string? newBio)
        {
            // Doesn't reset the Bio string's default, if it is over 250 char.
            if (newBio?.Length > 250)
            {
                // Invalid bio, throw an exception.
                throw new ArgumentOutOfRangeException(nameof(newBio), "Setting a bio requires a non-null string between 1-250 characters long");
            }

            // Reset Bio string to default value, if empty, whitespace, or null value
            // "Clears" the field.
            if (string.IsNullOrWhiteSpace(newBio))
            {
                newBio = null;
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

        // Sprint 6, CSP-200: Filters active users by case-insensitive DisplayName substring match,
        // then sorts so exact full matches appear before substring matches, both groups alphabetically.
        // Email is never searched. Users with the placeholder DisplayName "UNSET" are excluded.
        public async Task<IEnumerable<ApplicationUser>> SearchUsersAsync(string query)
        {
            var queryLower = query.ToLower();
            var matches = (await _userRepo.GetAllAsync(
                u => !u.IsDeactivated
                     && u.DisplayName != null
                     && u.DisplayName != "UNSET"
                     && u.DisplayName.ToLower().Contains(queryLower)
            )).ToList();

            // Full matches (case-insensitive) come first, then substring matches.
            // Within each group, sort alphabetically by display name.
            return matches
                .OrderBy(u => u.DisplayName!.Equals(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase);
        }

        public async Task UpdateDisplayNameAsync(ApplicationUser user, string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName) || displayName.Length < 2 || displayName.Length > 50)
                throw new ArgumentOutOfRangeException(nameof(displayName), "Display name must be between 2 and 50 characters.");

            user.DisplayName = displayName;
            await user.SaveModelAsync(_userRepo);
        }

        public async Task<int> GetTotalUserCountAsync()
        {
            // Count only active (non-deactivated) users.
            return (await _userRepo.GetAllAsync(u => !u.IsDeactivated)).Count();
        }

        // Sprint 7, CSP-179: Soft-lock a user out of their account, 
        // by taking the input value of statusFlag and flipping IsDeactivated.
        public async Task<bool> LockToggleAccountAsync(ApplicationUser user, bool statusFlag)
        {
            if (user == null)
            {
                // Cannot lock or open a nonexistent account.
                return false;
            }

            user.AccountLocked = statusFlag;
            await user.SaveModelAsync(_userRepo);

            return true;
        }
    }
}
