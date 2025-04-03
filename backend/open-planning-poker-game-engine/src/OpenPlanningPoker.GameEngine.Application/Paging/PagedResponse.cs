namespace OpenPlanningPoker.GameEngine.Application.Paging;

public class PagedResponse<T>
{
    /// <summary>
    /// Paged items
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Total items count
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Current page
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Indicator if there is next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Indicator if there is previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}