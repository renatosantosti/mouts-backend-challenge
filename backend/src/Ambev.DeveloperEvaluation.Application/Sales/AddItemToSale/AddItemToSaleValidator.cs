using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;

public sealed class AddItemToSaleValidator : AbstractValidator<AddItemToSaleCommand>
{
    public AddItemToSaleValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
    }
}
