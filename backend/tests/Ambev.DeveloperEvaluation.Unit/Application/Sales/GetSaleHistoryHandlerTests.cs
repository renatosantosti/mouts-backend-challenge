using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.GetSaleHistory;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class GetSaleHistoryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsReaderResult()
    {
        var reader = Substitute.For<ISaleEventHistoryReader>();
        var saleId = Guid.NewGuid();
        IReadOnlyList<SaleHistoryEvent> expected =
        [
            new()
            {
                SaleId = saleId,
                EventType = "SaleCreatedEvent",
                OccurredOn = DateTime.UtcNow
            }
        ];

        reader.ListBySaleIdAsync(saleId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var sut = new GetSaleHistoryHandler(reader);
        var result = await sut.Handle(new GetSaleHistoryQuery(saleId), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }
}
