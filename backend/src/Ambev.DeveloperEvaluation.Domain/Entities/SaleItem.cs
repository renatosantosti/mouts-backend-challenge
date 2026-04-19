using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Line item of a sale. Discount and totals are computed in <see cref="Recalculate"/>.
/// </summary>
public class SaleItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    private SaleItem()
    {
    }

    internal SaleItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        IsCancelled = false;
        Recalculate();
    }

    private void Recalculate()
    {
        if (IsCancelled)
        {
            Discount = 0;
            TotalAmount = 0;
            return;
        }

        ValidateQuantity();
        ValidateUnitPrice();

        var lineSubtotal = RoundMoney(Quantity * UnitPrice);
        Discount = ComputeDiscount(Quantity, lineSubtotal);
        TotalAmount = RoundMoney(lineSubtotal - Discount);
    }

    internal void Cancel()
    {
        IsCancelled = true;
        Recalculate();
    }

    /// <summary>
    /// Merges an additional quantity into this line, updates unit price and name from the latest command, and recalculates amounts.
    /// </summary>
    internal void ConsolidateWith(int additionalQuantity, decimal unitPrice, string productName)
    {
        if (IsCancelled)
            throw new DomainException("Cannot consolidate into a cancelled sale item.");

        if (additionalQuantity < 1)
            throw new DomainException("Quantity must be at least 1.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        var newQuantity = Quantity + additionalQuantity;
        if (newQuantity > 20)
            throw new DomainException("Cannot sell more than 20 identical items.");

        Quantity = newQuantity;
        UnitPrice = unitPrice;
        ProductName = productName;
        Recalculate();
    }

    private void ValidateQuantity()
    {
        if (Quantity < 1)
            throw new DomainException("Quantity must be at least 1.");

        if (Quantity > 20)
            throw new DomainException("Cannot sell more than 20 identical items.");
    }

    private void ValidateUnitPrice()
    {
        if (UnitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");
    }

    private static decimal ComputeDiscount(int quantity, decimal lineSubtotal)
    {
        if (quantity < 4)
            return 0;

        if (quantity <= 9)
            return RoundMoney(lineSubtotal * 0.10m);

        return RoundMoney(lineSubtotal * 0.20m);
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
