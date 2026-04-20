namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public sealed class SaleHistoryEvent
{
    public Guid SaleId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; }
    public string? SaleNumber { get; init; }
    public decimal? TotalAmount { get; init; }
    public Guid? SaleItemId { get; init; }
}
