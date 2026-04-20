namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

/// <summary>
/// API response model for sale details.
/// </summary>
public sealed class SaleResponse
{
    /// <summary>
    /// Unique identifier of the sale.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Business number of the sale.
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

    /// <summary>
    /// Current total amount of the sale.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Indicates whether the sale is cancelled.
    /// </summary>
    public bool IsCancelled { get; init; }

    /// <summary>
    /// Items currently associated with the sale.
    /// </summary>
    public IReadOnlyList<SaleItemResponse> Items { get; init; } = [];
}
