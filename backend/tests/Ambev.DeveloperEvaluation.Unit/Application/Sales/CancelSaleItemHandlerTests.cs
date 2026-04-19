using System.Linq;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
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

public class CancelSaleItemHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CancelSaleItemHandler _handler;

    public CancelSaleItemHandlerTests()
    {
        _handler = new CancelSaleItemHandler(_saleRepository, _mapper, NullLogger<CancelSaleItemHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenSaleMissing_ThrowsKeyNotFoundException()
    {
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdForUpdateAsync(saleId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(null));

        var act = () => _handler.Handle(new CancelSaleItemCommand(saleId, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenItemCancelled_PersistsAndDoesNotUseReadOnlyLoader()
    {
        var sale = SaleTestData.CreateValidSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, "Product", 2, 10m);
        var itemId = sale.Items.Single(i => i.ProductId == productId).Id;

        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sale));
        var dto = new SaleResponse { Id = sale.Id };
        _mapper.Map<SaleResponse>(sale).Returns(dto);

        var result = await _handler.Handle(new CancelSaleItemCommand(sale.Id, itemId), CancellationToken.None);

        result.Should().BeSameAs(dto);
        sale.Items.Single(i => i.Id == itemId).IsCancelled.Should().BeTrue();
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        await _saleRepository.DidNotReceive().GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemIdUnknown_ThrowsDomainException_AndDoesNotUpdate()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.AddItem(Guid.NewGuid(), "Product", 1, 10m);

        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));

        var act = () => _handler.Handle(new CancelSaleItemCommand(sale.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}
