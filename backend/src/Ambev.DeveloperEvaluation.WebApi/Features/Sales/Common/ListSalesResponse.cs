namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

/// <summary>
/// API response model for paginated sales listing.
/// </summary>
public sealed class ListSalesResponse
{
    /// <summary>
    /// Current page data.
    /// </summary>
    public IReadOnlyList<SaleResponse> Data { get; init; } = [];

    /// <summary>
    /// Total number of items matching the query.
    /// </summary>
    public int TotalItems { get; init; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Total number of pages for the query.
    /// </summary>
    public int TotalPages { get; init; }
}
