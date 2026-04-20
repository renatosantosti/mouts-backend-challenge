using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CreateSaleValidatorTests
{
    [Fact]
    public async Task Validate_InvalidCommand_HasErrors()
    {
        var validator = new CreateSaleValidator();
        var command = SaleTestData.GenerateCreateSaleCommand(
            saleNumber: string.Empty,
            customerId: Guid.Empty,
            customerName: string.Empty,
            branchId: Guid.Empty,
            branchName: string.Empty);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
