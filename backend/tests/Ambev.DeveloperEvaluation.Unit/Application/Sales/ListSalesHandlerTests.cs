using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class ListSalesHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ListSalesHandler _handler;

    public ListSalesHandlerTests()
    {
        _handler = new ListSalesHandler(_saleRepository, _mapper);
    }

    [Fact]
    public async Task Handle_InvalidQuery_ThrowsValidationException()
    {
        var act = () => _handler.Handle(new ListSalesQuery(Page: 0, Size: 10), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        await _saleRepository.DidNotReceive().ListAsync(Arg.Any<SaleListQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidQuery_CallsListAsyncWithMappedDomainQuery_AndReturnsMappedPage()
    {
        var customerId = Guid.NewGuid();
        var filters = new Dictionary<string, string> { ["customerId"] = customerId.ToString() };
        var query = new ListSalesQuery(Page: 2, Size: 15, Order: "date_desc", filters);

        var sale = SaleTestData.CreateValidSale();
        var items = new List<Sale> { sale };
        const int totalCount = 42;

        _saleRepository
            .ListAsync(
                Arg.Is<SaleListQuery>(q =>
                    q.Page == 2
                    && q.Size == 15
                    && q.Order == "date_desc"
                    && q.Filters != null
                    && q.Filters["customerId"] == customerId.ToString()),
                Arg.Any<CancellationToken>())
            .Returns((items, totalCount));

        var mapped = new List<SaleResponse> { new() { Id = sale.Id, SaleNumber = sale.SaleNumber } };
        _mapper.Map<IReadOnlyList<SaleResponse>>(items).Returns(mapped);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeSameAs(mapped);
        result.TotalCount.Should().Be(totalCount);
        result.Page.Should().Be(2);
        result.Size.Should().Be(15);
        await _saleRepository.Received(1).ListAsync(Arg.Any<SaleListQuery>(), Arg.Any<CancellationToken>());
    }
}
