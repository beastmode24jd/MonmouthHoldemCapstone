using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using MH.Capstone.Domain.Migrations;
using System.Reflection.Metadata;

namespace MH.Capstone.Domain.Services
{
    public class ClubService : IClubService
    {
        /* IMPORTANT FUTURE CLUB SERVICE FILE NOTE!!!

            "Deleting a user will now throw if they still have club memberships or messages.
            Your service layer will need to clean those up before deleting a user."

        */
        private readonly IRepository<Badge, ApplicationDbContext> _badgeRepo;
        private readonly IRepository<UserBadge, ApplicationDbContext> _userBadgeRepo;
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _userRepo;
        private readonly INotificationService _notificationService;

        public ClubService(IRepository<Badge, ApplicationDbContext> badgeRepo,
        IRepository<UserBadge, ApplicationDbContext> userBadgeRepo,
        IRepository<ApplicationUser, ApplicationDbContext> userRepo,
        INotificationService notificationService)
        {
            // Switched Dependency Injection of DB context fully over to Repository structure
            _badgeRepo = badgeRepo;
            _userBadgeRepo = userBadgeRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
        }
    }
}