using Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class AddItemToSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AddItemToSaleHandler _handler;

    public AddItemToSaleHandlerTests()
    {
        _handler = new AddItemToSaleHandler(_saleRepository, _mapper, NullLogger<AddItemToSaleHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenSaleMissing_ThrowsKeyNotFoundException()
    {
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdForUpdateAsync(saleId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(null));

        var command = new AddItemToSaleCommand(saleId, Guid.NewGuid(), "Product", 1, 10m);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAddingExistingProduct_ConsolidatesLineAndUsesLatestPriceAndName()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "Old Name", 2, 10m);

        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sale));
        _mapper.Map<SaleResponse>(Arg.Any<Sale>()).Returns(new SaleResponse { Id = sale.Id });

        var command = new AddItemToSaleCommand(sale.Id, productId, "New Name", 3, 12m);

        await _handler.Handle(command, CancellationToken.None);

        await _saleRepository.DidNotReceive().GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        sale.Items.Should().ContainSingle(i => i.ProductId == productId && !i.IsCancelled);
        var item = sale.Items.Single(i => i.ProductId == productId && !i.IsCancelled);
        item.Quantity.Should().Be(5);
        item.UnitPrice.Should().Be(12m);
        item.ProductName.Should().Be("New Name");
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQuantityExceedsLimit_ThrowsDomainException()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "Product", 20, 10m);

        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));

        var command = new AddItemToSaleCommand(sale.Id, productId, "Product", 1, 10m);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaleCancelled_ThrowsDomainException()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.Cancel();

        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));

        var command = new AddItemToSaleCommand(sale.Id, Guid.NewGuid(), "Product", 1, 10m);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}
