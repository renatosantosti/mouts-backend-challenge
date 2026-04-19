using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

internal static class SaleDomainEventLogger
{
    public static void LogAndClear(ILogger logger, Sale sale)
    {
        foreach (var domainEvent in sale.DomainEvents)
        {
            switch (domainEvent)
            {
                case SaleCreatedEvent e:
                    logger.LogInformation(
                        "Domain event SaleCreated: SaleId={SaleId}, SaleNumber={SaleNumber}, OccurredOn={OccurredOn}",
                        e.SaleId, e.SaleNumber, e.OccurredOn);
                    break;
                case SaleModifiedEvent e:
                    logger.LogInformation(
                        "Domain event SaleModified: SaleId={SaleId}, TotalAmount={TotalAmount}, OccurredOn={OccurredOn}",
                        e.SaleId, e.TotalAmountAfterChange, e.OccurredOn);
                    break;
                case SaleCancelledEvent e:
                    logger.LogInformation(
                        "Domain event SaleCancelled: SaleId={SaleId}, OccurredOn={OccurredOn}",
                        e.SaleId, e.OccurredOn);
                    break;
                case ItemCancelledEvent e:
                    logger.LogInformation(
                        "Domain event ItemCancelled: SaleId={SaleId}, SaleItemId={SaleItemId}, OccurredOn={OccurredOn}",
                        e.SaleId, e.SaleItemId, e.OccurredOn);
                    break;
                default:
                    logger.LogInformation("Domain event {EventType}", domainEvent.GetType().Name);
                    break;
            }
        }

        sale.ClearDomainEvents();
    }
}
