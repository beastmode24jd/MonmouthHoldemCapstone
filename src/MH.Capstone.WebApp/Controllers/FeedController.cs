using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers
{
    // CSP-187 AC1: activity feed of sightings from followed users, newest first,
    // with blocked authors filtered out.
    [Authorize]
    [Route("Feed")]
    public class FeedController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFollowService _followService;
        private readonly IBlockService _blockService;
        private readonly ISightingsService _sightingsService;

        public FeedController(
            UserManager<ApplicationUser> userManager,
            IFollowService followService,
            IBlockService blockService,
            ISightingsService sightingsService)
        {
            _userManager = userManager;
            _followService = followService;
            _blockService = blockService;
            _sightingsService = sightingsService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var viewer = await _userManager.GetUserAsync(User);
            if (viewer == null) return Challenge();

            var viewerId = viewer.GuidId;

            var followeeIds = (await _followService.GetFolloweeIdsAsync(viewerId)).ToHashSet();
            var model = new FeedViewModel { HasFollowees = followeeIds.Count > 0 };

            if (followeeIds.Count == 0) return View(model);

            var blockedIds = (await _blockService.GetBlockedUserIdsAsync(viewerId)).ToHashSet();
            var allSightings = await _sightingsService.GetAllSightingsAsync();

            model.Sightings = allSightings
                .Where(s =>
                {
                    var ownerId = Guid.Parse(s.UserIdentityId);
                    return followeeIds.Contains(ownerId) && !blockedIds.Contains(ownerId);
                })
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new SightingCardViewModel(s))
                .ToList();

            return View(model);
        }
    }
}
