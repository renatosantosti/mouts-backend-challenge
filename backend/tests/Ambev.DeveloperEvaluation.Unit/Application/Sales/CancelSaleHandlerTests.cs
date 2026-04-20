using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CancelSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ILogger<CancelSaleHandler> _logger = Substitute.For<ILogger<CancelSaleHandler>>();
    private readonly CancelSaleHandler _handler;

    public CancelSaleHandlerTests()
    {
        _handler = new CancelSaleHandler(_saleRepository, _mapper, _logger);
    }

    [Fact]
    public async Task Handle_WhenSaleMissing_ThrowsKeyNotFoundException()
    {
        var saleId = Guid.NewGuid();
        _saleRepository.GetByIdForUpdateAsync(saleId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(null));

        var act = () => _handler.Handle(new CancelSaleCommand(saleId), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSaleFound_CancelsPersistsAndDoesNotUseReadOnlyLoader()
    {
        var sale = SaleTestData.CreateValidSale();
        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));
        _saleRepository.UpdateAsync(sale, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sale));
        var dto = new SaleResponse { Id = sale.Id, SaleNumber = sale.SaleNumber };
        _mapper.Map<SaleResponse>(sale).Returns(dto);

        var result = await _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        result.Should().BeSameAs(dto);
        sale.IsCancelled.Should().BeTrue();
        sale.DomainEvents.Should().BeEmpty();
        await _saleRepository.Received(1).UpdateAsync(sale, Arg.Any<CancellationToken>());
        await _saleRepository.DidNotReceive().GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("Simulated broker publish SaleCancelled")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyCancelled_ThrowsDomainException_AndDoesNotUpdate()
    {
        var sale = SaleTestData.CreateValidSale();
        sale.Cancel();

        _saleRepository.GetByIdForUpdateAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));

        var act = () => _handler.Handle(new CancelSaleCommand(sale.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _saleRepository.DidNotReceive().UpdateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }
}
