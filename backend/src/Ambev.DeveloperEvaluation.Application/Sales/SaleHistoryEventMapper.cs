using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Application.Sales;

internal static class SaleHistoryEventMapper
{
    public static IReadOnlyList<SaleHistoryEvent> Map(IReadOnlyCollection<IDomainEvent> domainEvents)
    {
        return domainEvents.Select(MapOne).ToArray();
    }

    private static SaleHistoryEvent MapOne(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            SaleCreatedEvent e => new SaleHistoryEvent
            {
                SaleId = e.SaleId,
                EventType = nameof(SaleCreatedEvent),
                OccurredOn = e.OccurredOn,
                SaleNumber = e.SaleNumber
            },
            SaleModifiedEvent e => new SaleHistoryEvent
            {
                SaleId = e.SaleId,
                EventType = nameof(SaleModifiedEvent),
                OccurredOn = e.OccurredOn,
                TotalAmount = e.TotalAmountAfterChange
            },
            SaleCancelledEvent e => new SaleHistoryEvent
            {
                SaleId = e.SaleId,
                EventType = nameof(SaleCancelledEvent),
                OccurredOn = e.OccurredOn
            },
            ItemCancelledEvent e => new SaleHistoryEvent
            {
                SaleId = e.SaleId,
                EventType = nameof(ItemCancelledEvent),
                OccurredOn = e.OccurredOn,
                SaleItemId = e.SaleItemId
            },
            _ => new SaleHistoryEvent
            {
                EventType = domainEvent.GetType().Name,
                OccurredOn = DateTime.UtcNow
            }
        };
    }
}
