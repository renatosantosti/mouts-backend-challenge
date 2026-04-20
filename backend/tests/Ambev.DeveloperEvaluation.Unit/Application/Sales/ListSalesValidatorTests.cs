using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class ListSalesValidatorTests
{
    [Fact]
    public async Task Validate_PageLessThanOne_HasErrors()
    {
        var validator = new ListSalesValidator();
        var query = new ListSalesQuery(Page: 0, Size: 10);

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
