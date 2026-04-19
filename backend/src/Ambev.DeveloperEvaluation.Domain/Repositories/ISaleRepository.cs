using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Persistence abstraction for <see cref="Sale"/> aggregate.
/// </summary>
public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy physical delete path. Business flows should prefer <c>Sale.Cancel()</c> for traceability.
    /// </summary>
    [Obsolete("Use sale cancellation in domain/application flows. Physical delete is legacy compatibility.")]
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists sales with pagination and optional ordering/filters (see <see cref="SaleListQuery"/>).
    /// </summary>
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        SaleListQuery query,
        CancellationToken cancellationToken = default);
}
