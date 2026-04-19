namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Query for listing sales (pagination, ordering, filters align with general-api conventions: _page, _size, _order, field filters).
/// </summary>
public sealed class SaleListQuery
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 10;
    public string? Order { get; init; }
    public IReadOnlyDictionary<string, string>? Filters { get; init; }
}
