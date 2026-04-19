using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.GetSaleById;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class GetSaleByIdHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetSaleByIdHandler _handler;

    public GetSaleByIdHandlerTests()
    {
        _handler = new GetSaleByIdHandler(_saleRepository, _mapper);
    }

    [Fact]
    public async Task Handle_WhenMissing_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _saleRepository.GetByIdReadOnlyAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(null));

        var act = () => _handler.Handle(new GetSaleByIdQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsMappedResponse()
    {
        var sale = SaleTestData.CreateValidSale();
        var dto = new SaleResponse { Id = sale.Id, SaleNumber = sale.SaleNumber };

        _saleRepository.GetByIdReadOnlyAsync(sale.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Sale?>(sale));
        _mapper.Map<SaleResponse>(sale).Returns(dto);

        var result = await _handler.Handle(new GetSaleByIdQuery(sale.Id), CancellationToken.None);

        result.Should().BeSameAs(dto);
        await _saleRepository.Received(1).GetByIdReadOnlyAsync(sale.Id, Arg.Any<CancellationToken>());
        await _saleRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
