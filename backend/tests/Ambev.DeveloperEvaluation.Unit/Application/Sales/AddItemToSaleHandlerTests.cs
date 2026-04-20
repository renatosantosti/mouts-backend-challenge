using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class AddItemToSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ISaleEventPublisher _eventPublisher = Substitute.For<ISaleEventPublisher>();
    private readonly ISaleEventHistoryRecorder _historyRecorder = Substitute.For<ISaleEventHistoryRecorder>();
    private readonly AddItemToSaleHandler _handler;

    public AddItemToSaleHandlerTests()
    {
        _handler = new AddItemToSaleHandler(_saleRepository, _mapper, _eventPublisher, _historyRecorder);
    }

    [Fact]
    public async Task Handle_WhenSaleMissing_ThrowsKeyNotFoundException()
    {
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdForUpdateAsync(saleId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(null));

        var command = SaleTestData.GenerateAddItemToSaleCommand(
            saleId: saleId,
            productName: "Product",
            quantity: 1,
            unitPrice: 10m);

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

        var command = SaleTestData.GenerateAddItemToSaleCommand(
            saleId: sale.Id,
            productId: productId,
            productName: "New Name",
            quantity: 3,
            unitPrice: 12m);

        await _handler.Handle(command, CancellationToken.None);

        await _saleRepository.DidNotReceive().GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        sale.Items.Should().ContainSingle(i => i.ProductId == productId && !i.IsCancelled);
        var item = sale.Items.Single(i => i.ProductId == productId && !i.IsCancelled);
        item.Quantity.Should().Be(5);
        item.UnitPrice.Should().Be(12m);
        item.ProductName.Should().Be("New Name");
        sale.DomainEvents.Should().BeEmpty();
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<IReadOnlyCollection<IDomainEvent>>(events => events.Count > 0),
            Arg.Any<CancellationToken>());
        await _historyRecorder.Received(1).RecordAsync(
            Arg.Is<IReadOnlyCollection<IDomainEvent>>(events => events.Count > 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQuantityExceedsLimit_ThrowsDomainException()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "Product", 20, 10m);

        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));

        var command = SaleTestData.GenerateAddItemToSaleCommand(
            saleId: sale.Id,
            productId: productId,
            productName: "Product",
            quantity: 1,
            unitPrice: 10m);

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

        var command = SaleTestData.GenerateAddItemToSaleCommand(
            saleId: sale.Id,
            productName: "Product",
            quantity: 1,
            unitPrice: 10m);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}
