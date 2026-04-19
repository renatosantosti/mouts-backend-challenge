namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public sealed class SaleItemResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public decimal TotalAmount { get; init; }
    public bool IsCancelled { get; init; }
}
