using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSaleById;

public sealed class GetSaleByIdValidator : AbstractValidator<GetSaleByIdQuery>
{
    public GetSaleByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
