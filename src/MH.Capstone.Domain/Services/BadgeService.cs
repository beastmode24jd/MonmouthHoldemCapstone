using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    public class BadgeService : IBadgeService
    {
        /* Update the user bio only if it is 250 characters or below.
        Removed from the ApplicationUser data model, for clarity and ease of
        EF Migrations and DB updates. */

        private readonly ApplicationDbContext _context;

        public BadgeService(ApplicationDbContext context)
        {
            // Dependency Injection of DB Context
            _context = context;
        }

        public async Task AddBadge(ApplicationUser user, int badgeID)
        {
            
        }

    }
}