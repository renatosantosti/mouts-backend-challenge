using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Persistence abstraction for <see cref="Sale"/> aggregate.
/// </summary>
/// <remarks>
/// Application code should load with <see cref="GetByIdReadOnlyAsync"/> for queries and with
/// <see cref="GetByIdForUpdateAsync"/> followed by <see cref="UpdateAsync"/> when mutating the aggregate.
/// </remarks>
public interface ISaleRepository
{
    Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads sale aggregate for command/update flows (tracking enabled).
    /// </summary>
    Task<Sale?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads sale aggregate for query/read-only flows (no tracking).
    /// </summary>
    Task<Sale?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists sales with pagination and optional ordering/filters (see <see cref="SaleListQuery"/>).
    /// </summary>
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        SaleListQuery query,
        CancellationToken cancellationToken = default);
}
