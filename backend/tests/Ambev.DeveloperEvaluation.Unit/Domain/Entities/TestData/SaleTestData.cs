using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>
/// Helpers for building <see cref="Sale"/> instances in tests.
/// </summary>
public static class SaleTestData
{
    public static Sale CreateValidSale(string? saleNumber = null)
    {
        return Sale.Create(
            saleNumber ?? "SALE-001",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Jane Customer",
            Guid.NewGuid(),
            "Branch SP");
    }
}
