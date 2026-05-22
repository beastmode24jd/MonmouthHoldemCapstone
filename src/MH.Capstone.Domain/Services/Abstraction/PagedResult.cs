namespace MH.Capstone.Domain.Services.Abstraction;

/// <summary>
/// CSP-199: One page of a larger result set, plus the metadata a paginated
/// view needs (total size, current position, and whether Prev/Next exist).
/// </summary>
/// <param name="Items">The items on this page — already ordered, at most <paramref name="PageSize"/> of them.</param>
/// <param name="TotalCount">Total number of items across ALL pages, not just this one.</param>
/// <param name="Page">The 1-based page number this result represents (already clamped to >= 1).</param>
/// <param name="PageSize">The maximum number of items a single page can hold.</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>Number of pages needed to hold <see cref="TotalCount"/> items; 0 when there are none.</summary>
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when a page exists before this one.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>True when a page exists after this one.</summary>
    public bool HasNextPage => Page < TotalPages;
}
