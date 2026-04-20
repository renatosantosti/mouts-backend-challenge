using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public sealed class SimulatedSalesEventBroker : ISaleEventPublisher
{
    private readonly ILogger<SimulatedSalesEventBroker> _logger;

    public SimulatedSalesEventBroker(ILogger<SimulatedSalesEventBroker> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (domainEvent)
            {
                case SaleCreatedEvent e:
                    _logger.LogInformation(
                        "Simulated broker publish SaleCreated: SaleId={SaleId}, SaleNumber={SaleNumber}, OccurredOn={OccurredOn}",
                        e.SaleId, e.SaleNumber, e.OccurredOn);
                    break;
                case SaleModifiedEvent e:
                    _logger.LogInformation(
                        "Simulated broker publish SaleModified: SaleId={SaleId}, TotalAmount={TotalAmount}, OccurredOn={OccurredOn}",
                        e.SaleId, e.TotalAmountAfterChange, e.OccurredOn);
                    break;
                case SaleCancelledEvent e:
                    _logger.LogInformation(
                        "Simulated broker publish SaleCancelled: SaleId={SaleId}, OccurredOn={OccurredOn}",
                        e.SaleId, e.OccurredOn);
                    break;
                case ItemCancelledEvent e:
                    _logger.LogInformation(
                        "Simulated broker publish ItemCancelled: SaleId={SaleId}, SaleItemId={SaleItemId}, OccurredOn={OccurredOn}",
                        e.SaleId, e.SaleItemId, e.OccurredOn);
                    break;
                default:
                    _logger.LogInformation(
                        "Simulated broker publish {EventType}",
                        domainEvent.GetType().Name);
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
