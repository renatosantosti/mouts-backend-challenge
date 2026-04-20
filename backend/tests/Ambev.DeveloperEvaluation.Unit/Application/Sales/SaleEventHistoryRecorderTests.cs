using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class SaleEventHistoryRecorderTests
{
    private readonly ISaleEventHistoryWriter _writer = Substitute.For<ISaleEventHistoryWriter>();
    private readonly ILogger<SaleEventHistoryRecorder> _logger = Substitute.For<ILogger<SaleEventHistoryRecorder>>();

    [Fact]
    public async Task RecordAsync_WithEvents_MapsAndWrites()
    {
        var saleId = Guid.NewGuid();
        IReadOnlyCollection<IDomainEvent> events =
        [
            new SaleCreatedEvent(saleId, "S-1", DateTime.UtcNow),
            new SaleModifiedEvent(saleId, DateTime.UtcNow, 99.9m)
        ];

        var sut = new SaleEventHistoryRecorder(_writer, _logger);
        await sut.RecordAsync(events, CancellationToken.None);

        await _writer.Received(1).AppendAsync(
            Arg.Is<IReadOnlyCollection<SaleHistoryEvent>>(x => x.Count == 2 && x.All(e => e.SaleId == saleId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_WhenWriterThrows_DoesNotThrow()
    {
        IReadOnlyCollection<IDomainEvent> events =
        [
            new SaleCancelledEvent(Guid.NewGuid(), DateTime.UtcNow)
        ];

        _writer.AppendAsync(Arg.Any<IReadOnlyCollection<SaleHistoryEvent>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("mongo failure"));

        var sut = new SaleEventHistoryRecorder(_writer, _logger);
        var act = () => sut.RecordAsync(events, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
