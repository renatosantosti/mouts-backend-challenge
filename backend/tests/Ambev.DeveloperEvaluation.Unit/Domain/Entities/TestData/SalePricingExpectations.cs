namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;

/// <summary>
/// Expected monetary outcomes for assertions. Mirrors README tier rules and <c>SaleItem</c> pricing;
/// update alongside domain when rules change.
/// </summary>
public static class SalePricingExpectations
{
    public static (decimal Discount, decimal LineTotal) ExpectedForActiveLine(int quantity, decimal unitPrice)
    {
        var lineSubtotal = RoundMoney(quantity * unitPrice);
        var discount = ComputeDiscount(quantity, lineSubtotal);
        var lineTotal = RoundMoney(lineSubtotal - discount);
        return (discount, lineTotal);
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
