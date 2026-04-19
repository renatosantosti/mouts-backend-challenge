namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed class SaleCancelledEvent : IDomainEvent
{
    public Guid SaleId { get; }
    public DateTime OccurredOn { get; }

    public SaleCancelledEvent(Guid saleId, DateTime occurredOn)
    {
        SaleId = saleId;
        OccurredOn = occurredOn;
    }
}
