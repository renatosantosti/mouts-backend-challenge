using Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Bogus;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>
/// Provides methods for generating sales test data using the Bogus library.
/// </summary>
public static class SaleTestData
{
    private static readonly Faker<CreateSaleCommand> CreateSaleCommandFaker = new Faker<CreateSaleCommand>()
        .CustomInstantiator(f => new CreateSaleCommand(
            $"SALE-{f.Random.AlphaNumeric(10).ToUpperInvariant()}",
            f.Date.RecentOffset(15).UtcDateTime,
            f.Random.Guid(),
            f.Person.FullName,
            f.Random.Guid(),
            $"Branch {f.Address.City()}"));

    private static readonly Faker<AddItemToSaleCommand> AddItemToSaleCommandFaker = new Faker<AddItemToSaleCommand>()
        .CustomInstantiator(f => new AddItemToSaleCommand(
            f.Random.Guid(),
            f.Random.Guid(),
            f.Commerce.ProductName(),
            f.Random.Int(1, 20),
            Math.Round(f.Random.Decimal(1, 300), 2, MidpointRounding.AwayFromZero)));

    public static Sale CreateValidSale(
        string? saleNumber = null,
        DateTime? date = null,
        Guid? customerId = null,
        string? customerName = null,
        Guid? branchId = null,
        string? branchName = null)
    {
        var generated = CreateSaleCommandFaker.Generate();

        return Sale.Create(
            saleNumber ?? generated.SaleNumber,
            date ?? generated.Date,
            customerId ?? generated.CustomerId,
            customerName ?? generated.CustomerName,
            branchId ?? generated.BranchId,
            branchName ?? generated.BranchName);
    }

    public static CreateSaleCommand GenerateCreateSaleCommand(
        string? saleNumber = null,
        DateTime? date = null,
        Guid? customerId = null,
        string? customerName = null,
        Guid? branchId = null,
        string? branchName = null)
    {
        var generated = CreateSaleCommandFaker.Generate();
        return generated with
        {
            SaleNumber = saleNumber ?? generated.SaleNumber,
            Date = date ?? generated.Date,
            CustomerId = customerId ?? generated.CustomerId,
            CustomerName = customerName ?? generated.CustomerName,
            BranchId = branchId ?? generated.BranchId,
            BranchName = branchName ?? generated.BranchName
        };
    }

    public static AddItemToSaleCommand GenerateAddItemToSaleCommand(
        Guid? saleId = null,
        Guid? productId = null,
        string? productName = null,
        int? quantity = null,
        decimal? unitPrice = null)
    {
        var generated = AddItemToSaleCommandFaker.Generate();
        return generated with
        {
            SaleId = saleId ?? generated.SaleId,
            ProductId = productId ?? generated.ProductId,
            ProductName = productName ?? generated.ProductName,
            Quantity = quantity ?? generated.Quantity,
            UnitPrice = unitPrice ?? generated.UnitPrice
        };
    }
}
