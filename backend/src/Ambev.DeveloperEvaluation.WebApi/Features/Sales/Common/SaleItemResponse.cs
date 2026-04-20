namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

/// <summary>
/// API response model for a sale item.
/// </summary>
public sealed class SaleItemResponse
{
    /// <summary>
    /// Unique identifier of the sale item.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// External identifier of the product.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Denormalized product name snapshot.
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Quantity for this sale line.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Unit price considered for this item.
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Discount amount applied to this line.
    /// </summary>
    public decimal Discount { get; init; }

    /// <summary>
    /// Final total amount of this line after discounts.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Indicates whether this line was cancelled.
    /// </summary>
    public bool IsCancelled { get; init; }
}
