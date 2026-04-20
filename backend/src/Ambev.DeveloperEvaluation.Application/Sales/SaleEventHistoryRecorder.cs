using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public sealed class SaleEventHistoryRecorder : ISaleEventHistoryRecorder
{
    private readonly ISaleEventHistoryWriter _writer;
    private readonly ILogger<SaleEventHistoryRecorder> _logger;

    public SaleEventHistoryRecorder(
        ISaleEventHistoryWriter writer,
        ILogger<SaleEventHistoryRecorder> logger)
    {
        _writer = writer;
        _logger = logger;
    }

    public async Task RecordAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        if (domainEvents.Count == 0)
            return;

        var events = SaleHistoryEventMapper.Map(domainEvents);

        try
        {
            await _writer.AppendAsync(events, cancellationToken);
        }
        catch (Exception ex)
        {
            var saleId = events.FirstOrDefault()?.SaleId;
            _logger.LogError(
                ex,
                "Could not persist sale history on MongoDB. SaleId={SaleId}, EventCount={EventCount}",
                saleId,
                events.Count);
        }
    }
}
