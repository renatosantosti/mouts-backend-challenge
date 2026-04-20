namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

/// <summary>
/// API request model for creating a sale header.
/// </summary>
public sealed class CreateSaleRequest
{
    /// <summary>
    /// Unique business number of the sale.
    /// </summary>
    public string SaleNumber { get; init; } = string.Empty;

    /// <summary>
    /// Date and time when the sale happened.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// External identifier of the customer.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Denormalized customer name snapshot.
    /// </summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>
    /// External identifier of the branch.
    /// </summary>
    public Guid BranchId { get; init; }

    /// <summary>
    /// Denormalized branch name snapshot.
    /// </summary>
    public string BranchName { get; init; } = string.Empty;
}
