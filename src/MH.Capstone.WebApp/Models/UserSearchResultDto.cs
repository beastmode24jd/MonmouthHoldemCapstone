// Sprint 5, CSP-54
namespace MH.Capstone.WebApp.Models;

public record UserSearchResultDto(string Id, string UserName, bool HasProfileImage);

public record UserSearchResponseDto(
    IEnumerable<UserSearchResultDto> Results,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
