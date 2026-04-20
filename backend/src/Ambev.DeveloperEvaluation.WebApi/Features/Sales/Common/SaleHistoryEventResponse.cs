namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

public sealed class SaleHistoryEventResponse
{
    public Guid SaleId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; }
    public string? SaleNumber { get; init; }
    public decimal? TotalAmount { get; init; }
    public Guid? SaleItemId { get; init; }
}
