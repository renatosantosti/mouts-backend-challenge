namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.AddItem;

/// <summary>
/// API request model for adding an item to an existing sale.
/// </summary>
public sealed class AddSaleItemRequest
{
    /// <summary>
    /// External identifier of the product to be added.
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Denormalized product name snapshot.
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Quantity to add for this product.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Unit price used for pricing and discount rules.
    /// </summary>
    public decimal UnitPrice { get; init; }
}
