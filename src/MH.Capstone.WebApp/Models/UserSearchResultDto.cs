// Sprint 5, CSP-54 — Sprint 6, CSP-200: DisplayName replaces UserName/email
namespace MH.Capstone.WebApp.Models;

public record UserSearchResultDto(string Id, string DisplayName, bool HasProfileImage);

public record UserSearchResponseDto(
    IEnumerable<UserSearchResultDto> Results,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
