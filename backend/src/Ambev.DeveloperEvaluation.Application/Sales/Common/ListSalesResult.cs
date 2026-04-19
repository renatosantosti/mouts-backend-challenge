namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public sealed class ListSalesResult
{
    public IReadOnlyList<SaleResponse> Items { get; init; } = Array.Empty<SaleResponse>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int Size { get; init; }
}
