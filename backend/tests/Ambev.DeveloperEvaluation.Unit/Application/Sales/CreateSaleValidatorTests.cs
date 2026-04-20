using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CreateSaleValidatorTests
{
    [Fact]
    public async Task Validate_InvalidCommand_HasErrors()
    {
        var validator = new CreateSaleValidator();
        var command = new CreateSaleCommand(string.Empty, DateTime.UtcNow, Guid.Empty, "", Guid.Empty, "");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
