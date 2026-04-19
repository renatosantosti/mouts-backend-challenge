using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        _handler = new CreateSaleHandler(_saleRepository, _mapper, NullLogger<CreateSaleHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAndReturnsMappedSale()
    {
        var command = new CreateSaleCommand(
            "SALE-UNIT-1",
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch");

        var expected = new SaleResponse { Id = Guid.NewGuid(), SaleNumber = command.SaleNumber };

        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<Sale>()));

        _mapper.Map<SaleResponse>(Arg.Any<Sale>()).Returns(expected);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _saleRepository.Received(1).CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
        _mapper.Received(1).Map<SaleResponse>(Arg.Any<Sale>());
    }

    [Fact]
    public async Task Handle_InvalidCommand_ThrowsValidationException()
    {
        var command = new CreateSaleCommand(string.Empty, DateTime.UtcNow, Guid.Empty, "", Guid.Empty, "");

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
