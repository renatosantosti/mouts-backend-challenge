namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

public sealed class SaleHistoryResponse
{
    public IReadOnlyList<SaleHistoryEventResponse> Data { get; init; } = [];
}
