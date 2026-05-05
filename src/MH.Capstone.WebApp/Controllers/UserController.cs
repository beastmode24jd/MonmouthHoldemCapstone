// Sprint 5, CSP-54
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MH.Capstone.WebApp.Controllers;

[Authorize]
[Route("user")]
public class UserController : Controller
{
    private const int PageSize = 10;

    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
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
}
