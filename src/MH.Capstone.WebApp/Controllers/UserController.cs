// Sprint 5, CSP-54
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers;

[Authorize]
[Route("user")]
public class UserController : Controller
{
    private const int PageSize = 10;

    private readonly IUserService _userService;
    private readonly IFollowService _followService;
    private readonly IBlockService _blockService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        IFollowService followService,
        IBlockService blockService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _followService = followService;
        _blockService = blockService;
        _userManager = userManager;
        _logger = logger;
    }

    // Sprint 5, CSP-54
    [HttpGet("search")]
    public IActionResult Search() => View();

    // Sprint 5, CSP-54: Returns a paginated JSON result of users matching the query.
    // Page size is fixed at 10. Empty/whitespace query returns an empty response immediately.
    [HttpGet("search/results")]
    [Produces("application/json")]
    public async Task<IActionResult> SearchResults([FromQuery] string? query, [FromQuery] int page = 1)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(new UserSearchResponseDto([], 0, 1, PageSize, 0));

        _logger.LogDebug("User search request: query={Query}, page={Page}", query, page);

        var allResults = (await _userService.SearchUsersAsync(query)).ToList();
        var totalCount = allResults.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)PageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));

        // Sprint 6, CSP-200: project DisplayName, not UserName/email
        var pageResults = allResults
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new UserSearchResultDto(u.Id, u.DisplayName, u.ProfileImage != null))
            .ToList();

        return Ok(new UserSearchResponseDto(pageResults, totalCount, page, PageSize, totalPages));
    }

    // Sprint 5, CSP-54: Serves a user's profile image by ID, or redirects to the default avatar if none is set.
    [HttpGet("{id:guid}/profile-image")]
    public async Task<IActionResult> ProfileImage(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user?.ProfileImage == null || string.IsNullOrEmpty(user.ProfileImageType))
            return Redirect("/imgs/profileDefault.jpg");

        return File(user.ProfileImage, user.ProfileImageType);
    }

    // CSP-187 AC1: follow another user. Redirects back to that user's profile.
    [HttpPost("{id:guid}/follow")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(Guid id)
    {
        var viewer = await _userManager.GetUserAsync(User);
        if (viewer == null) return Challenge();
        try { await _followService.FollowAsync(viewer.GuidId, id); }
        catch (InvalidOperationException ex) { TempData["FollowError"] = ex.Message; }
        return RedirectToAction("Index", "Account", new { id });
    }

    [HttpPost("{id:guid}/unfollow")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow(Guid id)
    {
        var viewer = await _userManager.GetUserAsync(User);
        if (viewer == null) return Challenge();
        await _followService.UnfollowAsync(viewer.GuidId, id);
        return RedirectToAction("Index", "Account", new { id });
    }

    // CSP-187 AC3: block another user (silent — no notification).
    [HttpPost("{id:guid}/block")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(Guid id)
    {
        var viewer = await _userManager.GetUserAsync(User);
        if (viewer == null) return Challenge();
        try { await _blockService.BlockAsync(viewer.GuidId, id); }
        catch (InvalidOperationException ex) { TempData["BlockError"] = ex.Message; }
        return RedirectToAction("Index", "Account", new { id });
    }

    [HttpPost("{id:guid}/unblock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(Guid id)
    {
        var viewer = await _userManager.GetUserAsync(User);
        if (viewer == null) return Challenge();
        await _blockService.UnblockAsync(viewer.GuidId, id);
        return RedirectToAction("Index", "Account", new { id });
    }
}
